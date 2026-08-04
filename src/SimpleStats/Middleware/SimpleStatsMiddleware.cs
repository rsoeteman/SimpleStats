using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleStats.Configuration;
using SimpleStats.Services;
using Umbraco.Cms.Core.Web;

namespace SimpleStats.Middleware;

/// <summary>
/// ASP.NET Core middleware that records a page view for the currently resolved Umbraco content
/// node once the response is about to be sent (so it reflects a successfully rendered request).
/// </summary>
public sealed class SimpleStatsMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleStatsMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public SimpleStatsMiddleware(RequestDelegate next) => _next = next;

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        ILogger<SimpleStatsMiddleware> logger,
        IUmbracoContextAccessor umbracoContextAccessor,
        ISimpleStatsService simpleStatsService,
        IOptionsMonitor<SimpleStatsSettings> settingsMonitor)
    {
        context.Response.OnStarting(() =>
        {
            try
            {
                if (umbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext? umbracoContext))
                {
                    Guid? nodeKey = umbracoContext.PublishedRequest?.PublishedContent?.Key;
                    if (nodeKey is { } key && key != Guid.Empty && !settingsMonitor.CurrentValue.ExcludedNodeIds.Contains(key))
                    {
                        simpleStatsService.Increment(key);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in {Middleware}", nameof(SimpleStatsMiddleware));
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
