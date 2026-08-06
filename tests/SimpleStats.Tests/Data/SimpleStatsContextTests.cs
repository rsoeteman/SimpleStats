using Microsoft.EntityFrameworkCore;
using SimpleStats.Data;
using SimpleStats.Tests.TestSupport;

namespace SimpleStats.Tests.Data;

public sealed class SimpleStatsContextTests
{
    [Fact]
    public void Schema_MapsPageViewsToExpectedTableName()
    {
        var (connection, context) = SqliteInMemoryContextFactory.CreateWithSchema();
        using var _ = connection;
        using var __ = context;

        string? tableName = context.Model.FindEntityType(typeof(SimpleStatsPageView))!.GetTableName();

        Assert.Equal("SimpleStatsPageViews", tableName);
    }

    [Fact]
    public async Task UniqueIndex_RejectsDuplicateNodeKeyAndDateCombination()
    {
        var (connection, context) = SqliteInMemoryContextFactory.CreateWithSchema();
        await using var _ = connection;
        await using var __ = context;

        Guid nodeKey = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        context.PageViews.Add(new SimpleStatsPageView { NodeKey = nodeKey, Date = today, Count = 1 });
        await context.SaveChangesAsync();

        context.PageViews.Add(new SimpleStatsPageView { NodeKey = nodeKey, Date = today, Count = 1 });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task UniqueIndex_AllowsSameNodeOnDifferentDays()
    {
        var (connection, context) = SqliteInMemoryContextFactory.CreateWithSchema();
        await using var _ = connection;
        await using var __ = context;

        Guid nodeKey = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        context.PageViews.Add(new SimpleStatsPageView { NodeKey = nodeKey, Date = today, Count = 1 });
        context.PageViews.Add(new SimpleStatsPageView { NodeKey = nodeKey, Date = today.AddDays(-1), Count = 1 });

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.PageViews.CountAsync());
    }
}
