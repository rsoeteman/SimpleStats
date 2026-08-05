using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleStats.Models;
using SimpleStats.OpenApi;
using SimpleStats.Services;
using Umbraco.Cms.Api.Common.Attributes;
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
[MapToApi(SimpleStatsApiConfiguration.ApiName)]
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
    /// Gets summarized page-view counts, one entry per distinct content node, for views recorded
    /// within the given inclusive date range.
    /// </summary>
    /// <param name="startDate">The inclusive start of the date range.</param>
    /// <param name="endDate">The inclusive end of the date range.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>One summary per distinct node, with counts summed across the range and ordered by view count descending, or a 400 response if <paramref name="startDate"/> is after <paramref name="endDate"/>.</returns>
    [HttpGet("page-views")]
    [MapToApiVersion("1.0")]
    public async Task<ActionResult<IEnumerable<PageViewSummary>>> GetPageViews(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        if (startDate > endDate)
        {
            return BadRequest("startDate must be less than or equal to endDate.");
        }

        IReadOnlyList<PageViewSummary> pageViews =
            await _simpleStatsService.GetPageViewsAsync(startDate, endDate, cancellationToken);

        return Ok(pageViews);
    }
}
