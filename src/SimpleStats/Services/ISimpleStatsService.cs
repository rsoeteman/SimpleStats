using SimpleStats.Data;

namespace SimpleStats.Services;

/// <summary>
/// Tracks and persists simple page-view statistics for Umbraco content nodes.
/// </summary>
public interface ISimpleStatsService
{
    /// <summary>
    /// Increments the in-memory view count for the given content node. Fast, non-blocking,
    /// does not touch the database.
    /// </summary>
    /// <param name="nodeKey">The Umbraco content node key.</param>
    void Increment(Guid nodeKey);

    /// <summary>
    /// Gets the total recorded view count for the given content node, including committed
    /// (persisted) counts. Does not include not-yet-committed in-memory increments.
    /// </summary>
    /// <param name="nodeKey">The Umbraco content node key.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The total number of recorded views.</returns>
    Task<int> GetTotalViewsAsync(Guid nodeKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes all pending in-memory view counts to the database, upserting per (node, day) bucket.
    /// Safe to call concurrently with <see cref="Increment"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task CommitStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all persisted page-view rows whose date falls within the given inclusive range.
    /// Does not include not-yet-committed in-memory increments.
    /// </summary>
    /// <param name="startDate">The inclusive start of the date range.</param>
    /// <param name="endDate">The inclusive end of the date range.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching page-view rows, ordered by date.</returns>
    Task<IReadOnlyList<SimpleStatsPageView>> GetPageViewsAsync(
        DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
}
