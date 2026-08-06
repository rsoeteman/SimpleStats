using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SimpleStats.Configuration;
using SimpleStats.Jobs;
using SimpleStats.Services;

namespace SimpleStats.Tests.Jobs;

public sealed class SimpleStatsCommitJobTests
{
    private readonly Mock<ISimpleStatsService> _simpleStatsService = new();
    private readonly Mock<ILogger<SimpleStatsCommitJob>> _logger = new();
    private readonly Mock<IOptionsMonitor<SimpleStatsSettings>> _settingsMonitor = new();
    private Action<SimpleStatsSettings, string?>? _onChangeCallback;

    public SimpleStatsCommitJobTests()
    {
        _settingsMonitor
            .Setup(m => m.OnChange(It.IsAny<Action<SimpleStatsSettings, string?>>()))
            .Callback<Action<SimpleStatsSettings, string?>>(callback => _onChangeCallback = callback)
            .Returns(Mock.Of<IDisposable>());
    }

    [Fact]
    public void Period_OnConstruction_ReflectsCurrentCommitInterval()
    {
        _settingsMonitor.Setup(m => m.CurrentValue).Returns(new SimpleStatsSettings { CommitIntervalSeconds = 30 });

        var job = CreateJob();

        Assert.Equal(TimeSpan.FromSeconds(30), job.Period);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Period_OnConstruction_ClampsNonPositiveIntervalToOneSecond(int commitIntervalSeconds)
    {
        _settingsMonitor.Setup(m => m.CurrentValue)
            .Returns(new SimpleStatsSettings { CommitIntervalSeconds = commitIntervalSeconds });

        var job = CreateJob();

        Assert.Equal(TimeSpan.FromSeconds(1), job.Period);
    }

    [Fact]
    public void SettingsChange_RecalculatesPeriodAndRaisesPeriodChanged()
    {
        _settingsMonitor.Setup(m => m.CurrentValue).Returns(new SimpleStatsSettings { CommitIntervalSeconds = 60 });
        var job = CreateJob();
        bool raised = false;
        job.PeriodChanged += (_, _) => raised = true;

        _onChangeCallback!.Invoke(new SimpleStatsSettings { CommitIntervalSeconds = 15 }, null);

        Assert.Equal(TimeSpan.FromSeconds(15), job.Period);
        Assert.True(raised);
    }

    [Fact]
    public async Task RunJobAsync_CallsCommitStatsAsyncOnTheService()
    {
        _settingsMonitor.Setup(m => m.CurrentValue).Returns(new SimpleStatsSettings());
        var job = CreateJob();

        await job.RunJobAsync();

        _simpleStatsService.Verify(s => s.CommitStatsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunJobAsync_WhenServiceThrows_ExceptionIsCaughtAndLogged()
    {
        _settingsMonitor.Setup(m => m.CurrentValue).Returns(new SimpleStatsSettings());
        _simpleStatsService
            .Setup(s => s.CommitStatsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var job = CreateJob();

        Exception? thrown = await Record.ExceptionAsync(() => job.RunJobAsync());

        Assert.Null(thrown);
        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private SimpleStatsCommitJob CreateJob() =>
        new(_simpleStatsService.Object, _logger.Object, _settingsMonitor.Object);
}
