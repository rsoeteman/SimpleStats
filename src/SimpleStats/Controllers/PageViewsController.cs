using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleStats.Data;
using SimpleStats.Services;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Routing;

namespace SimpleStats.Controllers;

/// <summary>
/// Backoffice API for reading recorded SimpleStats page-view data.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[BackOfficeRoute("simplestats/api/v{version:apiVersion}")]
[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
public sealed class PageViewsController : ControllerBase
{
    private readonly ISimpleStatsService _simpleStatsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PageViewsController"/> class.
    /// </summary>
    /// <param name="simpleStatsService">The service used to read persisted page-view data.</param>
    public PageViewsController(ISimpleStatsService simpleStatsService) =>
        _simpleStatsService = simpleStatsService;

    /// <summary>
    /// Gets recorded page views whose date falls within the given inclusive range.
    /// </summary>
    /// <param name="startDate">The inclusive start of the date range.</param>
    /// <param name="endDate">The inclusive end of the date range.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching page views, or a 400 response if <paramref name="startDate"/> is after <paramref name="endDate"/>.</returns>
    [HttpGet("page-views")]
    [MapToApiVersion("1.0")]
    public async Task<ActionResult<IEnumerable<SimpleStatsPageView>>> GetPageViews(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        if (startDate > endDate)
        {
            return BadRequest("startDate must be less than or equal to endDate.");
        }

        IReadOnlyList<SimpleStatsPageView> pageViews =
            await _simpleStatsService.GetPageViewsAsync(startDate, endDate, cancellationToken);

        return Ok(pageViews);
    }
}
