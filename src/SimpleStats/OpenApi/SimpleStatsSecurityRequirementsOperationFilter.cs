using Umbraco.Cms.Api.Management.OpenApi;

namespace SimpleStats.OpenApi;

/// <summary>
/// Applies back office security requirements to operations mapped to the SimpleStats Swagger document.
/// </summary>
public sealed class SimpleStatsSecurityRequirementsOperationFilter : BackOfficeSecurityRequirementsOperationFilterBase
{
    /// <inheritdoc />
    protected override string ApiName => SimpleStatsApiConfiguration.ApiName;
}
