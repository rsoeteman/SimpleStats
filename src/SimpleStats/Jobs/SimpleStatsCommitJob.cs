using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleStats.Configuration;
using SimpleStats.Services;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace SimpleStats.Jobs;

/// <summary>
/// Recurring background job that periodically flushes in-memory SimpleStats page-view counts
/// to the database.
/// </summary>
public sealed class SimpleStatsCommitJob : IRecurringBackgroundJob
{
    private readonly ISimpleStatsService _simpleStatsService;
    private readonly ILogger<SimpleStatsCommitJob> _logger;
    private TimeSpan _period;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleStatsCommitJob"/> class.
    /// </summary>
    public SimpleStatsCommitJob(
        ISimpleStatsService simpleStatsService,
        ILogger<SimpleStatsCommitJob> logger,
        IOptionsMonitor<SimpleStatsSettings> settingsMonitor)
    {
        _simpleStatsService = simpleStatsService;
        _logger = logger;
        _period = GetPeriod(settingsMonitor.CurrentValue);

        settingsMonitor.OnChange(settings =>
        {
            _period = GetPeriod(settings);
            PeriodChanged.Invoke(this, EventArgs.Empty);
        });
    }

    /// <inheritdoc />
    public TimeSpan Period => _period;

    /// <inheritdoc />
    public event EventHandler PeriodChanged = delegate { };

    /// <inheritdoc />
    public async Task RunJobAsync()
    {
        try
        {
            await _simpleStatsService.CommitStatsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing SimpleStats page views");
        }
    }

    private static TimeSpan GetPeriod(SimpleStatsSettings settings) =>
        TimeSpan.FromSeconds(Math.Max(1, settings.CommitIntervalSeconds));
}
