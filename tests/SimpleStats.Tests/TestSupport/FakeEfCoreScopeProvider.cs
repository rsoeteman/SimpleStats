using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace SimpleStats.Tests.TestSupport;

/// <summary>
/// <see cref="IEFCoreScopeProvider{TDbContext}"/> test double that hands out a
/// <see cref="FakeEfCoreScope{TDbContext}"/> wrapping a single shared <typeparamref name="TDbContext"/>
/// instance, so state written in one scope is visible to the next (mirroring how
/// <c>SimpleStatsService</c> opens a fresh scope per call against the same underlying database).
/// </summary>
internal sealed class FakeEfCoreScopeProvider<TDbContext> : IEFCoreScopeProvider<TDbContext>
    where TDbContext : DbContext
{
    private readonly TDbContext _context;

    public FakeEfCoreScopeProvider(TDbContext context) => _context = context;

    public IEFCoreScope<TDbContext> CreateScope(
        RepositoryCacheMode repositoryCacheMode = RepositoryCacheMode.Unspecified, bool? scopeFileSystems = null) =>
        new FakeEfCoreScope<TDbContext>(_context);

    public IEFCoreScope<TDbContext> CreateDetachedScope(
        RepositoryCacheMode repositoryCacheMode = RepositoryCacheMode.Unspecified, bool? scopeFileSystems = null) =>
        new FakeEfCoreScope<TDbContext>(_context);

    public void AttachScope(IEFCoreScope<TDbContext> other) => throw new NotImplementedException();

    public IEFCoreScope<TDbContext> DetachScope() => throw new NotImplementedException();

    public IScopeContext? AmbientScopeContext => null;
}
