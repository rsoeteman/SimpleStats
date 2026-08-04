using Microsoft.EntityFrameworkCore;

namespace SimpleStats.Data;

/// <summary>
/// EF Core database context for the SimpleStats package.
/// </summary>
public sealed class SimpleStatsContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleStatsContext"/> class.
    /// </summary>
    /// <param name="options">The context options, configured via <c>AddUmbracoDbContext</c>.</param>
    public SimpleStatsContext(DbContextOptions<SimpleStatsContext> options)
        : base(options)
    {
    }

    /// <summary>Gets the set of recorded, committed page-view counts.</summary>
    public DbSet<SimpleStatsPageView> PageViews => Set<SimpleStatsPageView>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SimpleStatsPageView>(entity =>
        {
            entity.ToTable("SimpleStatsPageViews");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.NodeKey).IsRequired();
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.Count).IsRequired();

            // One row per (node, day); also the natural lookup/upsert path used by
            // SimpleStatsService.CommitStatsAsync, so make it a real unique index rather than
            // relying on application-level checks alone.
            entity.HasIndex(e => new { e.NodeKey, e.Date })
                .IsUnique()
                .HasDatabaseName("IX_SimpleStatsPageViews_NodeKey_Date");
        });
    }
}
