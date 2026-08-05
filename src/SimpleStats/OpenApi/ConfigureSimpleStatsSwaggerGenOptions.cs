using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimpleStats.OpenApi;

/// <summary>
/// Registers the SimpleStats Swagger document and its security operation filter.
/// </summary>
public sealed class ConfigureSimpleStatsSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
{
    /// <inheritdoc />
    public void Configure(SwaggerGenOptions options)
    {
        options.SwaggerDoc(
            SimpleStatsApiConfiguration.ApiName,
            new OpenApiInfo
            {
                Title = "SimpleStats API",
                Version = "1.0",
                Description = "API for reading SimpleStats page-view data.",
            });

        options.OperationFilter<SimpleStatsSecurityRequirementsOperationFilter>();
    }
}
