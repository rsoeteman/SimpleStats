namespace SimpleStats.Configuration;

/// <summary>
/// Configuration options for the SimpleStats package.
/// Bind from <c>appsettings.json</c> under the key <c>"SimpleStats"</c>.
/// </summary>
public sealed class SimpleStatsSettings
{
    /// <summary>Gets the configuration section name used in <c>appsettings.json</c>.</summary>
    public const string SectionName = "SimpleStats";

    /// <summary>Gets or sets the content node keys excluded from page-view tracking (for example, a custom error page).</summary>
    public Guid[] ExcludedNodeIds { get; set; } = [];

    /// <summary>Gets or sets how often, in seconds, buffered view counts are committed to the database.</summary>
    public int CommitIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets how long, in seconds, to wait after application start-up before buffered view
    /// counts are committed for the first time. Defaults to 180, the same as Umbraco's
    /// <c>IRecurringBackgroundJob.DefaultDelay</c>. Read once at start-up.
    /// </summary>
    public int CommitDelaySeconds { get; set; } = 180;
}
