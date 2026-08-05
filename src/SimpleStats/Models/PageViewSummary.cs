namespace SimpleStats.Models;

/// <summary>
/// A summarized page-view count for a single Umbraco content node over a date range.
/// </summary>
public sealed class PageViewSummary
{
    /// <summary>Gets or sets the Umbraco content node key.</summary>
    public Guid NodeKey { get; set; }

    /// <summary>
    /// Gets or sets the content node's name, or "(Unknown)" if the node could no longer be resolved
    /// (e.g. deleted or unpublished with no draft).
    /// </summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the total number of views recorded for <see cref="NodeKey"/>.</summary>
    public int Count { get; set; }
}
