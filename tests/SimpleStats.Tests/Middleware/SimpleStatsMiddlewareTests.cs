using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SimpleStats.Configuration;
using SimpleStats.Middleware;
using SimpleStats.Services;
using SimpleStats.Tests.TestSupport;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;

namespace SimpleStats.Tests.Middleware;

public sealed class SimpleStatsMiddlewareTests
{
    private readonly Mock<ISimpleStatsService> _simpleStatsService = new();
    private readonly Mock<IUmbracoContextAccessor> _umbracoContextAccessor = new();
    private readonly Mock<IOptionsMonitor<SimpleStatsSettings>> _settingsMonitor = new();
    private readonly Mock<ILogger<SimpleStatsMiddleware>> _logger = new();

    public SimpleStatsMiddlewareTests() =>
        _settingsMonitor.Setup(m => m.CurrentValue).Returns(new SimpleStatsSettings());

    [Fact]
    public async Task InvokeAsync_ForResolvedNonExcludedNode_IncrementsThatNode()
    {
        Guid nodeKey = Guid.NewGuid();
        SetUpResolvedContent(nodeKey);

        await InvokeAndFireOnStartingAsync();

        _simpleStatsService.Verify(s => s.Increment(nodeKey), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_AlwaysCallsNextMiddleware()
    {
        bool nextCalled = false;
        SetUpResolvedContent(Guid.NewGuid());

        await InvokeAndFireOnStartingAsync(next: _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenNoUmbracoContext_DoesNotIncrement()
    {
        IUmbracoContext? none = null;
        _umbracoContextAccessor.Setup(a => a.TryGetUmbracoContext(out none)).Returns(false);

        await InvokeAndFireOnStartingAsync();

        _simpleStatsService.Verify(s => s.Increment(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenNoPublishedRequest_DoesNotIncrement()
    {
        var umbracoContext = new Mock<IUmbracoContext>();
        umbracoContext.Setup(c => c.PublishedRequest).Returns((IPublishedRequest?)null);
        IUmbracoContext? resolved = umbracoContext.Object;
        _umbracoContextAccessor.Setup(a => a.TryGetUmbracoContext(out resolved)).Returns(true);

        await InvokeAndFireOnStartingAsync();

        _simpleStatsService.Verify(s => s.Increment(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenNodeKeyIsExcluded_DoesNotIncrement()
    {
        Guid excludedKey = Guid.NewGuid();
        _settingsMonitor.Setup(m => m.CurrentValue).Returns(new SimpleStatsSettings { ExcludedNodeIds = [excludedKey] });
        SetUpResolvedContent(excludedKey);

        await InvokeAndFireOnStartingAsync();

        _simpleStatsService.Verify(s => s.Increment(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenIncrementThrows_ExceptionIsCaughtAndLoggedNotPropagated()
    {
        SetUpResolvedContent(Guid.NewGuid());
        _simpleStatsService.Setup(s => s.Increment(It.IsAny<Guid>())).Throws(new InvalidOperationException("boom"));

        Exception? thrown = await Record.ExceptionAsync(() => InvokeAndFireOnStartingAsync());

        Assert.Null(thrown);
        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private void SetUpResolvedContent(Guid nodeKey)
    {
        var publishedContent = new Mock<IPublishedContent>();
        publishedContent.Setup(c => c.Key).Returns(nodeKey);

        var publishedRequest = new Mock<IPublishedRequest>();
        publishedRequest.Setup(r => r.PublishedContent).Returns(publishedContent.Object);

        var umbracoContext = new Mock<IUmbracoContext>();
        umbracoContext.Setup(c => c.PublishedRequest).Returns(publishedRequest.Object);

        IUmbracoContext? resolved = umbracoContext.Object;
        _umbracoContextAccessor.Setup(a => a.TryGetUmbracoContext(out resolved)).Returns(true);
    }

    private async Task InvokeAndFireOnStartingAsync(RequestDelegate? next = null)
    {
        var responseFeature = new RecordingHttpResponseFeature();
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(responseFeature);

        var middleware = new SimpleStatsMiddleware(next ?? (_ => Task.CompletedTask));

        await middleware.InvokeAsync(
            context,
            _logger.Object,
            _umbracoContextAccessor.Object,
            _simpleStatsService.Object,
            _settingsMonitor.Object);

        await responseFeature.FireOnStartingAsync();
    }
}
