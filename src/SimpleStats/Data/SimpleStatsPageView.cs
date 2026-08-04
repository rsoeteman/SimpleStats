namespace SimpleStats.Data;

/// <summary>
/// A persisted, daily-bucketed page-view count for a single Umbraco content node.
/// </summary>
public sealed class SimpleStatsPageView
{
    /// <summary>Gets or sets the primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the Umbraco content node key (<c>IPublishedContent.Key</c>) the views were recorded against.</summary>
    public Guid NodeKey { get; set; }

    /// <summary>Gets or sets the UTC date the count applies to.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the number of views recorded for <see cref="NodeKey"/> on <see cref="Date"/>.</summary>
    public int Count { get; set; }
}
