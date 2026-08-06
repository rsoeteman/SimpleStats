using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace SimpleStats.Tests.TestSupport;

/// <summary>
/// Minimal <see cref="IEfCoreScope{TDbContext}"/> test double that runs
/// <see cref="ExecuteWithContextAsync{T}(Func{TDbContext, Task{T}})"/> directly against a real,
/// caller-owned <typeparamref name="TDbContext"/> instance instead of Umbraco's ambient scope
/// machinery. Members outside what <c>SimpleStatsService</c> actually uses (locking, isolated
/// caches, scope attach/detach) are intentionally unimplemented so a test fails loudly if it
/// starts depending on them.
/// </summary>
internal sealed class FakeEfCoreScope<TDbContext> : IEfCoreScope<TDbContext>
    where TDbContext : DbContext
{
    private readonly TDbContext _context;

    public FakeEfCoreScope(TDbContext context) => _context = context;

    public bool Completed { get; private set; }

    public IScopeContext? ScopeContext { get; set; }

    public Task<T> ExecuteWithContextAsync<T>(Func<TDbContext, Task<T>> method) => method(_context);

    public Task ExecuteWithContextAsync<T>(Func<TDbContext, Task> method) => method(_context);

    public bool Complete()
    {
        Completed = true;
        return true;
    }

    public void Dispose()
    {
        // The shared DbContext is owned and disposed by the test, not by the scope.
    }

    IScopedNotificationPublisher IEfCoreScope<TDbContext>.Notifications => throw new NotImplementedException();

    IScopedNotificationPublisher ICoreScope.Notifications => throw new NotImplementedException();

    public ILockingMechanism Locks => throw new NotImplementedException();

    public RepositoryCacheMode RepositoryCacheMode => RepositoryCacheMode.Unspecified;

    public IsolatedCaches IsolatedCaches => throw new NotImplementedException();

    public Guid InstanceId => Guid.Empty;

    public int CreatedThreadId => Environment.CurrentManagedThreadId;

    public void ReadLock(params int[] lockIds)
    {
    }

    public void WriteLock(params int[] lockIds)
    {
    }

    public void WriteLock(TimeSpan timeout, int lockId)
    {
    }

    public void ReadLock(TimeSpan timeout, int lockId)
    {
    }

    public void EagerWriteLock(params int[] lockIds)
    {
    }

    public void EagerWriteLock(TimeSpan timeout, int lockId)
    {
    }

    public void EagerReadLock(TimeSpan timeout, int lockId)
    {
    }

    public void EagerReadLock(params int[] lockIds)
    {
    }
}
