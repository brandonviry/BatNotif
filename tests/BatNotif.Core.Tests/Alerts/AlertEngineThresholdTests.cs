using BatNotif.Core.Alerts;

namespace BatNotif.Core.Tests.Alerts;

/// <summary>Seuils modifiés à chaud depuis la fenêtre principale (US2) : réévaluation immédiate.</summary>
public class AlertEngineThresholdTests : EngineTestBase
{
    [Fact]
    public void Lowering_high_threshold_below_level_enters_alert_immediately()
    {
        Start(level: 70, ac: true);
        Assert.Equal(AlertState.Normal, State);

        Engine.UpdateThresholds(20, 65);

        Assert.Equal(AlertState.AlertHigh, State);
        Assert.True(Engine.Output.ShouldPresent);
    }

    [Fact]
    public void Raising_high_threshold_well_above_level_ends_alert_immediately()
    {
        Start(level: 85, ac: true);
        Assert.Equal(AlertState.AlertHigh, State);

        Engine.UpdateThresholds(20, 90);

        Assert.Equal(AlertState.Normal, State);
    }

    [Fact]
    public void Raising_low_threshold_above_level_on_battery_enters_alert()
    {
        Start(level: 30, ac: false);

        Engine.UpdateThresholds(35, 80);

        Assert.Equal(AlertState.AlertLow, State);
    }

    [Fact]
    public void Unchanged_thresholds_do_not_raise_output_changed()
    {
        Start(level: 50, ac: false);
        var count = Outputs.Count;

        Engine.UpdateThresholds(20, 80);

        Assert.Equal(count, Outputs.Count);
    }
}
