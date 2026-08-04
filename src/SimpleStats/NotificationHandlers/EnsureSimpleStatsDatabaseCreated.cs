using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using SimpleStats.Data;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace SimpleStats.NotificationHandlers;

/// <summary>
/// Ensures the SimpleStats database table exists when the Umbraco application starts.
/// </summary>
/// <remarks>
/// Does not use <see cref="DatabaseFacade.EnsureCreatedAsync(CancellationToken)"/>: per EF Core's
/// own documentation, "if any tables exist (including tables for another DbContext class), the
/// schema won't be initialized." Since <c>SimpleStatsContext</c> shares Umbraco's own database
/// (<c>shareUmbracoConnection: true</c>), Umbraco's tables already exist, so
/// <c>EnsureCreatedAsync</c> is always a no-op here — it never creates <c>SimpleStatsPageViews</c>.
/// Nor does it use EF Core Migrations: migrations generated against one relational provider (e.g.
/// SQL Server) are not portable to another (e.g. SQLite) — applying them raises a
/// <c>PendingModelChangesWarning</c> at startup because the recorded model snapshot doesn't match
/// the active provider's schema representation. Instead, this checks for this context's own table
/// directly and creates just its tables via <see cref="IRelationalDatabaseCreator"/> when missing,
/// per EF Core's documented pattern for multiple DbContexts sharing one database. The tradeoff is
/// it cannot incrementally alter an already-existing table if the entity model changes later; that
/// would require switching to provider-specific migrations (mirroring how Umbraco itself ships
/// separate <c>Umbraco.Cms.Persistence.EFCore.SqlServer</c> / <c>.Sqlite</c> packages).
/// </remarks>
public sealed class EnsureSimpleStatsDatabaseCreated : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IDbContextFactory<SimpleStatsContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnsureSimpleStatsDatabaseCreated"/> class.
    /// </summary>
    public EnsureSimpleStatsDatabaseCreated(IDbContextFactory<SimpleStatsContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    /// <inheritdoc />
    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        await using SimpleStatsContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (await TableExistsAsync(db, cancellationToken))
        {
            return;
        }

        IRelationalDatabaseCreator databaseCreator = db.GetService<IRelationalDatabaseCreator>();
        await databaseCreator.CreateTablesAsync(cancellationToken);
    }

    private static async Task<bool> TableExistsAsync(SimpleStatsContext db, CancellationToken cancellationToken)
    {
        try
        {
            await db.PageViews.AnyAsync(cancellationToken);
            return true;
        }
        catch (DbException)
        {
            return false;
        }
    }
}
