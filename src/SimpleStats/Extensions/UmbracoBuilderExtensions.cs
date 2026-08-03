using Microsoft.Extensions.DependencyInjection;
using SimpleStats.Configuration;
using Umbraco.Cms.Core.DependencyInjection;

namespace SimpleStats.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> to register SimpleStats services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Registers all SimpleStats services and configuration with the Umbraco DI container.
    /// </summary>
    /// <param name="builder">The <see cref="IUmbracoBuilder"/> instance.</param>
    /// <returns>The same <see cref="IUmbracoBuilder"/> to allow chaining.</returns>
    public static IUmbracoBuilder AddSimpleStats(this IUmbracoBuilder builder)
    {
        builder.Services.AddOptions<SimpleStatsSettings>()
            .BindConfiguration(SimpleStatsSettings.SectionName);

        return builder;
    }
}
