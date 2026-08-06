using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using SimpleStats.Data;
using SimpleStats.Services;
using SimpleStats.Tests.TestSupport;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;

namespace SimpleStats.Tests.Services;

public sealed class SimpleStatsServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SimpleStatsContext _context;
    private readonly Mock<IPublishedContentCache> _publishedContentCache = new();
    private readonly SimpleStatsService _sut;

    public SimpleStatsServiceTests()
    {
        (_connection, _context) = SqliteInMemoryContextFactory.CreateWithSchema();
        var scopeProvider = new FakeEfCoreScopeProvider<SimpleStatsContext>(_context);
        _sut = new SimpleStatsService(scopeProvider, _publishedContentCache.Object, NullLogger());
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetTotalViewsAsync_WithOnlyPendingIncrements_ReturnsPendingCount()
    {
        Guid nodeKey = Guid.NewGuid();

        _sut.Increment(nodeKey);
        _sut.Increment(nodeKey);
        _sut.Increment(nodeKey);

        int total = await _sut.GetTotalViewsAsync(nodeKey);

        Assert.Equal(3, total);
    }

    [Fact]
    public async Task GetTotalViewsAsync_CombinesCommittedAndPendingCounts()
    {
        Guid nodeKey = Guid.NewGuid();
        _sut.Increment(nodeKey);
        _sut.Increment(nodeKey);
        await _sut.CommitStatsAsync();

        _sut.Increment(nodeKey);

        int total = await _sut.GetTotalViewsAsync(nodeKey);

        Assert.Equal(3, total);
    }

    [Fact]
    public async Task CommitStatsAsync_WithNoPendingIncrements_DoesNotThrowOrWriteRows()
    {
        await _sut.CommitStatsAsync();

        Assert.Empty(_context.PageViews);
    }

    [Fact]
    public async Task CommitStatsAsync_CreatesNewRowForFirstIncrementOfTheDay()
    {
        Guid nodeKey = Guid.NewGuid();
        _sut.Increment(nodeKey);
        _sut.Increment(nodeKey);

        await _sut.CommitStatsAsync();

        SimpleStatsPageView row = Assert.Single(_context.PageViews);
        Assert.Equal(nodeKey, row.NodeKey);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), row.Date);
        Assert.Equal(2, row.Count);
    }

    [Fact]
    public async Task CommitStatsAsync_IncrementsExistingSameDayRowInsteadOfDuplicating()
    {
        Guid nodeKey = Guid.NewGuid();
        _sut.Increment(nodeKey);
        await _sut.CommitStatsAsync();

        _sut.Increment(nodeKey);
        _sut.Increment(nodeKey);
        await _sut.CommitStatsAsync();

        SimpleStatsPageView row = Assert.Single(_context.PageViews);
        Assert.Equal(3, row.Count);
    }

    [Fact]
    public async Task CommitStatsAsync_TracksMultipleNodesIndependently()
    {
        Guid nodeA = Guid.NewGuid();
        Guid nodeB = Guid.NewGuid();
        _sut.Increment(nodeA);
        _sut.Increment(nodeB);
        _sut.Increment(nodeB);

        await _sut.CommitStatsAsync();

        Assert.Equal(1, _context.PageViews.Single(x => x.NodeKey == nodeA).Count);
        Assert.Equal(2, _context.PageViews.Single(x => x.NodeKey == nodeB).Count);
    }

    [Fact]
    public async Task GetPageViewsAsync_SumsPerNodeAcrossRangeAndOrdersByCountDescending()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Guid popular = Guid.NewGuid();
        Guid quiet = Guid.NewGuid();
        Guid outOfRange = Guid.NewGuid();

        _context.PageViews.AddRange(
            new SimpleStatsPageView { NodeKey = popular, Date = today, Count = 5 },
            new SimpleStatsPageView { NodeKey = popular, Date = today.AddDays(-1), Count = 4 },
            new SimpleStatsPageView { NodeKey = quiet, Date = today, Count = 2 },
            new SimpleStatsPageView { NodeKey = outOfRange, Date = today.AddDays(-10), Count = 100 });
        await _context.SaveChangesAsync();

        _publishedContentCache
            .Setup(c => c.GetById(true, popular))
            .Returns(PublishedContentNamed("Popular Page"));
        _publishedContentCache
            .Setup(c => c.GetById(true, quiet))
            .Returns(PublishedContentNamed("Quiet Page"));

        var results = await _sut.GetPageViewsAsync(today.AddDays(-1), today);

        Assert.Equal(2, results.Count);
        Assert.Equal(popular, results[0].NodeKey);
        Assert.Equal(9, results[0].Count);
        Assert.Equal("Popular Page", results[0].NodeName);
        Assert.Equal(quiet, results[1].NodeKey);
        Assert.Equal(2, results[1].Count);
    }

    [Fact]
    public async Task GetPageViewsAsync_WhenNodeCannotBeResolved_UsesUnknownFallbackName()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Guid deletedNode = Guid.NewGuid();
        _context.PageViews.Add(new SimpleStatsPageView { NodeKey = deletedNode, Date = today, Count = 1 });
        await _context.SaveChangesAsync();

        _publishedContentCache.Setup(c => c.GetById(true, deletedNode)).Returns((IPublishedContent?)null);

        var results = await _sut.GetPageViewsAsync(today, today);

        Assert.Equal("(Unknown)", Assert.Single(results).NodeName);
    }

    private static IPublishedContent PublishedContentNamed(string name)
    {
        var mock = new Mock<IPublishedContent>();
        mock.Setup(c => c.Name).Returns(name);
        return mock.Object;
    }

    private static ILogger<SimpleStatsService> NullLogger() =>
        new Mock<ILogger<SimpleStatsService>>().Object;
}
