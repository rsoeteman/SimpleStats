using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SimpleStats.Data;
using SimpleStats.NotificationHandlers;
using Umbraco.Cms.Core.Notifications;

namespace SimpleStats.Tests.NotificationHandlers;

public sealed class EnsureSimpleStatsDatabaseCreatedTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<SimpleStatsContext> _options = null!;

    public Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<SimpleStatsContext>().UseSqlite(_connection).Options;
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _connection.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task HandleAsync_WhenTableDoesNotExist_CreatesIt()
    {
        var handler = new EnsureSimpleStatsDatabaseCreated(new FakeDbContextFactory(_options));

        await handler.HandleAsync(new UmbracoApplicationStartedNotification(false), CancellationToken.None);

        await using var context = new SimpleStatsContext(_options);
        int count = await context.PageViews.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task HandleAsync_WhenTableAlreadyExists_DoesNotRecreateOrDropExistingData()
    {
        await using (var seedContext = new SimpleStatsContext(_options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.PageViews.Add(new SimpleStatsPageView
            {
                NodeKey = Guid.NewGuid(),
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Count = 42,
            });
            await seedContext.SaveChangesAsync();
        }

        var handler = new EnsureSimpleStatsDatabaseCreated(new FakeDbContextFactory(_options));
        await handler.HandleAsync(new UmbracoApplicationStartedNotification(false), CancellationToken.None);

        await using var context = new SimpleStatsContext(_options);
        SimpleStatsPageView row = await context.PageViews.SingleAsync();
        Assert.Equal(42, row.Count);
    }

    private sealed class FakeDbContextFactory : IDbContextFactory<SimpleStatsContext>
    {
        private readonly DbContextOptions<SimpleStatsContext> _options;

        public FakeDbContextFactory(DbContextOptions<SimpleStatsContext> options) => _options = options;

        public SimpleStatsContext CreateDbContext() => new(_options);

        public Task<SimpleStatsContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
