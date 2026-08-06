using System.Text.Json;
using SimpleStats.Configuration;

namespace SimpleStats.Tests.Configuration;

public sealed class SimpleStatsSettingsTests
{
    [Fact]
    public void Defaults_MatchDocumentedBehavior()
    {
        var settings = new SimpleStatsSettings();

        Assert.Empty(settings.ExcludedNodeIds);
        Assert.Equal(60, settings.CommitIntervalSeconds);
    }

    [Fact]
    public void AppsettingsSchema_DeclaredDefaultsAndConstraints_MatchTheSettingsClass()
    {
        string schemaPath = Path.Combine(AppContext.BaseDirectory, "appsettings-schema.simplestats.json");
        using JsonDocument schema = JsonDocument.Parse(File.ReadAllText(schemaPath));

        JsonElement settingsSchema = schema.RootElement
            .GetProperty("definitions")
            .GetProperty("SimpleStatsSettings")
            .GetProperty("properties");

        var defaults = new SimpleStatsSettings();

        JsonElement commitInterval = settingsSchema.GetProperty("CommitIntervalSeconds");
        Assert.Equal(defaults.CommitIntervalSeconds, commitInterval.GetProperty("default").GetInt32());
        Assert.Equal(1, commitInterval.GetProperty("minimum").GetInt32());

        JsonElement excludedNodeIds = settingsSchema.GetProperty("ExcludedNodeIds");
        Assert.Empty(excludedNodeIds.GetProperty("default").EnumerateArray());
        Assert.Empty(defaults.ExcludedNodeIds);
    }
}
