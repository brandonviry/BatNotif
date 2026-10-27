using BatNotif.Core.Alerts;
using BatNotif.Core.Logging;

namespace BatNotif.Core.Tests.Alerts;

/// <summary>Règles d'entrée / sortie d'alerte (FR-006 à FR-009, data-model §3).</summary>
public class AlertEngineTransitionTests : EngineTestBase
{
    [Fact]
    public void On_battery_at_low_threshold_enters_AlertLow()
    {
        Start(level: 21, ac: false);
        Assert.Equal(AlertState.Normal, State);

        Battery.SetLevel(20);

        Assert.Equal(AlertState.AlertLow, State);
        Assert.True(Engine.Output.ShouldPresent);
    }

    [Fact]
    public void On_ac_but_discharging_at_low_threshold_enters_AlertLow()
    {
        Start(level: 25, ac: true);

        Battery.Push(level: 19, acPresent: true, charging: false, discharging: true);

        Assert.Equal(AlertState.AlertLow, State);
    }

    [Fact]
    public void On_ac_at_high_threshold_enters_AlertHigh()
    {
        Start(level: 79, ac: true);

        Battery.SetLevel(80);

        Assert.Equal(AlertState.AlertHigh, State);
        Assert.True(Engine.Output.ShouldPresent);
    }

    [Fact]
    public void AlertLow_ends_at_low_reset()
    {
        Start(level: 20, ac: false);
        Assert.Equal(AlertState.AlertLow, State);

        Battery.SetLevel(21);
        Assert.Equal(AlertState.AlertLow, State);

        Battery.SetLevel(22);
        Assert.Equal(AlertState.Normal, State);
        Assert.False(Engine.Output.ShouldPresent);
    }

    [Fact]
    public void AlertLow_ends_when_plugged_and_charging_after_debounce()
    {
        Start(level: 15, ac: false);

        Battery.Push(level: 15, acPresent: true, charging: true, discharging: false);
        Assert.Equal(AlertState.AlertLow, State);

        Time.Advance(Debounce);
        Assert.Equal(AlertState.Normal, State);
    }

    [Fact]
    public void AlertHigh_ends_at_high_reset()
    {
        Start(level: 80, ac: true);
        Assert.Equal(AlertState.AlertHigh, State);

        Battery.SetLevel(79);
        Assert.Equal(AlertState.AlertHigh, State);

        Battery.SetLevel(78);
        Assert.Equal(AlertState.Normal, State);
    }

    [Fact]
    public void AlertHigh_ends_when_unplugged_after_debounce()
    {
        Start(level: 85, ac: true);

        Battery.SetAc(false);
        Assert.Equal(AlertState.AlertHigh, State);

        Time.Advance(Debounce);
        Assert.Equal(AlertState.Normal, State);
    }

    [Fact]
    public void High_level_on_battery_is_normal()
    {
        Start(level: 82, ac: false);

        Assert.Equal(AlertState.Normal, State);
        Assert.False(Engine.Output.ShouldPresent);
    }

    [Fact]
    public void Low_level_on_ac_while_charging_is_normal()
    {
        Start(level: 10, ac: true);

        Assert.Equal(AlertState.Normal, State);
    }

    [Fact]
    public void Oscillation_below_high_threshold_after_alert_end_does_not_retrigger()
    {
        Start(level: 80, ac: true);
        Battery.SetLevel(78);
        Assert.Equal(AlertState.Normal, State);
        var alertsBefore = Outputs.Count(o => o.State == AlertState.AlertHigh);

        for (var i = 0; i < 30; i++)
        {
            Battery.SetLevel(i % 2 == 0 ? 79 : 78);
        }

        Assert.Equal(AlertState.Normal, State);
        Assert.Equal(alertsBefore, Outputs.Count(o => o.State == AlertState.AlertHigh));
    }

    [Fact]
    public void Oscillation_around_high_threshold_during_alert_keeps_a_single_alert()
    {
        Start(level: 79, ac: true);

        for (var i = 0; i < 30; i++)
        {
            Battery.SetLevel(i % 2 == 0 ? 80 : 79);
        }

        Assert.Equal(AlertState.AlertHigh, State);
        Assert.Single(Log.OfKind(LogEventKind.StateChanged), e => e.Detail.Contains("AlertHigh"));
    }

    [Fact]
    public void Micro_cut_of_ac_does_not_end_AlertHigh()
    {
        Start(level: 85, ac: true);

        Battery.SetAc(false);
        Time.Advance(TimeSpan.FromSeconds(1));
        Battery.SetAc(true);
        Time.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(AlertState.AlertHigh, State);
    }

    [Fact]
    public void No_battery_gives_NoBattery_without_presentation()
    {
        Start(level: 0, ac: true, hasBattery: false);

        Assert.Equal(AlertState.NoBattery, State);
        Assert.False(Engine.Output.ShouldPresent);
    }

    [Fact]
    public void Battery_disappearing_during_alert_ends_it()
    {
        Start(level: 85, ac: true);

        Battery.Push(level: 0, acPresent: true, hasBattery: false);

        Assert.Equal(AlertState.NoBattery, State);
        Assert.False(Engine.Output.ShouldPresent);
    }

    [Fact]
    public void Transitions_are_logged_with_level_and_ac()
    {
        Start(level: 79, ac: true);

        Battery.SetLevel(80);

        var entry = Assert.Single(Log.OfKind(LogEventKind.StateChanged), e => e.Detail.Contains("AlertHigh"));
        Assert.Contains("Normal → AlertHigh", entry.Detail);
        Assert.Contains("80 %", entry.Detail);
    }
}
