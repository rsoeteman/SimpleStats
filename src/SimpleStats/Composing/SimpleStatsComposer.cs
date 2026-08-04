using SimpleStats.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace SimpleStats.Composing;

/// <summary>
/// Automatically registers SimpleStats services when the package is installed, via Umbraco's
/// composer discovery. Delegates entirely to <see cref="UmbracoBuilderExtensions.AddSimpleStats"/>.
/// </summary>
public sealed class SimpleStatsComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder) => builder.AddSimpleStats();
}
