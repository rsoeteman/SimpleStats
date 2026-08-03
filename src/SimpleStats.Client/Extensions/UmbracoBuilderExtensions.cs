using Umbraco.Cms.Core.DependencyInjection;

namespace SimpleStats.Client.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> to register the SimpleStats backoffice client.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Registers the SimpleStats backoffice extension.
    /// The frontend assets and <c>umbraco-package.manifest</c> are served as
    /// static web assets from <c>App_Plugins/SimpleStats/</c>.
    /// </summary>
    /// <param name="builder">The <see cref="IUmbracoBuilder"/> instance.</param>
    /// <returns>The same <see cref="IUmbracoBuilder"/> to allow chaining.</returns>
    public static IUmbracoBuilder AddSimpleStatsClient(this IUmbracoBuilder builder)
    {
        // In Umbraco 14+, package manifests are auto-discovered from the
        // umbraco-package.manifest JSON file served via static web assets.
        // No additional C# registration is required here.
        return builder;
    }
}
