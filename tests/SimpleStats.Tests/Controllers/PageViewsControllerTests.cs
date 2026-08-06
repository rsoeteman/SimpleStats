using Microsoft.AspNetCore.Mvc;
using Moq;
using SimpleStats.Controllers;
using SimpleStats.Models;
using SimpleStats.Services;

namespace SimpleStats.Tests.Controllers;

public sealed class PageViewsControllerTests
{
    private readonly Mock<ISimpleStatsService> _simpleStatsService = new();
    private readonly PageViewsController _sut;

    public PageViewsControllerTests() => _sut = new PageViewsController(_simpleStatsService.Object);

    [Fact]
    public async Task GetPageViews_WhenStartDateAfterEndDate_ReturnsBadRequest()
    {
        var startDate = new DateOnly(2026, 1, 10);
        var endDate = new DateOnly(2026, 1, 1);

        ActionResult<IEnumerable<PageViewSummary>> result =
            await _sut.GetPageViews(startDate, endDate, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        _simpleStatsService.Verify(
            s => s.GetPageViewsAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPageViews_WithValidRange_ReturnsOkWithServiceResult()
    {
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 1, 10);
        var expected = new List<PageViewSummary>
        {
            new() { NodeKey = Guid.NewGuid(), NodeName = "Home", Count = 5 },
        };
        _simpleStatsService
            .Setup(s => s.GetPageViewsAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        ActionResult<IEnumerable<PageViewSummary>> result =
            await _sut.GetPageViews(startDate, endDate, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, okResult.Value);
    }

    [Fact]
    public async Task GetPageViews_WhenStartDateEqualsEndDate_IsAllowed()
    {
        var date = new DateOnly(2026, 1, 1);
        _simpleStatsService
            .Setup(s => s.GetPageViewsAsync(date, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        ActionResult<IEnumerable<PageViewSummary>> result =
            await _sut.GetPageViews(date, date, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
