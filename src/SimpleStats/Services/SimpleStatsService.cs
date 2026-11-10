using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleStats.Data;
using SimpleStats.Models;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace SimpleStats.Services;

/// <summary>
/// Default <see cref="ISimpleStatsService"/> implementation. Registered as a singleton: the
/// in-memory counters must survive across requests, but all database access goes through
/// <see cref="IEFCoreScopeProvider{TDbContext}"/> on demand rather than holding a captured,
/// request-scoped <see cref="SimpleStatsContext"/> — injecting a scoped DbContext into a
/// singleton would be a captive-dependency bug and would not participate correctly in
/// Umbraco's ambient scope/transaction per request. <see cref="IPublishedContentCache"/> is also
/// registered as a singleton by Umbraco core, so it can be injected here directly without the
/// same concern.
/// </summary>
public sealed class SimpleStatsService : ISimpleStatsService
{
    private readonly IEFCoreScopeProvider<SimpleStatsContext> _scopeProvider;
    private readonly IPublishedContentCache _publishedContentCache;
    private readonly ILogger<SimpleStatsService> _logger;
    private readonly ConcurrentDictionary<Guid, int> _pendingCounts = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleStatsService"/> class.
    /// </summary>
    public SimpleStatsService(
        IEFCoreScopeProvider<SimpleStatsContext> scopeProvider,
        IPublishedContentCache publishedContentCache,
        ILogger<SimpleStatsService> logger)
    {
        _scopeProvider = scopeProvider;
        _publishedContentCache = publishedContentCache;
        _logger = logger;
    }

    /// <inheritdoc />
    public void Increment(Guid nodeKey) =>
        _pendingCounts.AddOrUpdate(nodeKey, addValue: 1, updateValueFactory: (_, current) => current + 1);

    /// <inheritdoc />
    public async Task<int> GetTotalViewsAsync(Guid nodeKey, CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<SimpleStatsContext> scope = _scopeProvider.CreateScope();
        int committed = await scope.ExecuteWithContextAsync(async (SimpleStatsContext db) =>
            await db.PageViews.Where(x => x.NodeKey == nodeKey).SumAsync(x => (int?)x.Count, cancellationToken) ?? 0);
        scope.Complete();

        _pendingCounts.TryGetValue(nodeKey, out int pending);
        return committed + pending;
    }

    /// <inheritdoc />
    public async Task CommitStatsAsync(CancellationToken cancellationToken = default)
    {
        if (_pendingCounts.IsEmpty)
        {
            return;
        }

        // Snapshot-and-remove per key so increments that arrive *during* the commit are not lost
        // and are not double-counted (avoids the reference implementation's non-atomic
        // Dictionary + partial lock race condition).
        var snapshot = new Dictionary<Guid, int>();
        foreach (Guid nodeKey in _pendingCounts.Keys)
        {
            if (_pendingCounts.TryRemove(nodeKey, out int count) && count > 0)
            {
                snapshot[nodeKey] = count;
            }
        }

        if (snapshot.Count == 0)
        {
            return;
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        using IEFCoreScope<SimpleStatsContext> scope = _scopeProvider.CreateScope();
        await scope.ExecuteWithContextAsync(async (SimpleStatsContext db) =>
        {
            foreach ((Guid nodeKey, int count) in snapshot)
            {
                SimpleStatsPageView? existing = await db.PageViews
                    .FirstOrDefaultAsync(x => x.NodeKey == nodeKey && x.Date == today, cancellationToken);

                if (existing is not null)
                {
                    existing.Count += count;
                }
                else
                {
                    db.PageViews.Add(new SimpleStatsPageView
                    {
                        NodeKey = nodeKey,
                        Date = today,
                        Count = count,
                    });
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            return true;
        });
        scope.Complete();

        _logger.LogDebug("SimpleStats committed pending view counts for {NodeCount} node(s)", snapshot.Count);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PageViewSummary>> GetPageViewsAsync(
        DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<SimpleStatsContext> scope = _scopeProvider.CreateScope();
        var grouped = await scope.ExecuteWithContextAsync(async (SimpleStatsContext db) =>
            await db.PageViews
                .Where(x => x.Date >= startDate && x.Date <= endDate)
                .GroupBy(x => x.NodeKey)
                .Select(g => new { NodeKey = g.Key, Count = g.Sum(x => x.Count) })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken));
        scope.Complete();

        return grouped
            .Select(x => new PageViewSummary
            {
                NodeKey = x.NodeKey,
                NodeName = _publishedContentCache.GetById(preview: true, contentId: x.NodeKey)?.Name ?? "(Unknown)",
                Count = x.Count,
            })
            .ToList();
    }
}
