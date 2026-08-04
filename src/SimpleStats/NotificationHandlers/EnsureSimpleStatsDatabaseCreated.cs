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
