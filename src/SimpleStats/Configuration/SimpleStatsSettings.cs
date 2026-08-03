namespace SimpleStats.Configuration;

/// <summary>
/// Configuration options for the SimpleStats package.
/// Bind from <c>appsettings.json</c> under the key <c>"SimpleStats"</c>.
/// </summary>
public sealed class SimpleStatsSettings
{
    /// <summary>Gets the configuration section name used in <c>appsettings.json</c>.</summary>
    public const string SectionName = "SimpleStats";

    /// <summary>Gets or sets the number of top pages to show in the dashboard widget.</summary>
    public int TopPagesCount { get; set; } = 10;

    /// <summary>Gets or sets the number of days of historical data to retain.</summary>
    public int RetentionDays { get; set; } = 90;
}
