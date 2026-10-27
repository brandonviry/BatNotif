using BatNotif.Core.Alerts;
using BatNotif.Core.Logging;

namespace BatNotif.Core.Tests.Alerts;

/// <summary>FR-027 : le snooze fait taire l'alerte 15 min, puis elle revient si la condition dure.</summary>
public class AlertEngineSnoozeTests : EngineTestBase
{
    private static readonly TimeSpan Snooze = AlertConstants.SnoozeDuration;

    [Fact]
    public void Snooze_silences_the_alert_for_15_minutes_then_it_comes_back()
    {
        Start(level: 85, ac: true);
        Assert.True(Engine.Output.ShouldPresent);

        Engine.Snooze();

        Assert.False(Engine.Output.ShouldPresent);
        Assert.Equal(AlertState.AlertHigh, State); // L'état réel reste suivi.
        Time.Advance(Snooze - TimeSpan.FromSeconds(1));
        Assert.False(Engine.Output.ShouldPresent);

        Time.Advance(TimeSpan.FromSeconds(1));
        Assert.True(Engine.Output.ShouldPresent);
        Assert.Null(Engine.Output.SnoozeUntil);
    }

    [Fact]
    public void Snooze_until_is_exposed()
    {
        Start(level: 85, ac: true);

        Engine.Snooze();

        Assert.Equal(Time.GetUtcNow() + Snooze, Engine.Output.SnoozeUntil);
        Assert.True(Engine.Output.IsSnoozed);
    }

    [Fact]
    public void Snooze_is_cancelled_when_the_alert_ends()
    {
        Start(level: 85, ac: true);
        Engine.Snooze();

        SetAcAndSettle(false); // Débranché : fin de l'alerte haute.

        Assert.Equal(AlertState.Normal, State);
        Assert.Null(Engine.Output.SnoozeUntil);

        // Rebranché avant la fin des 15 min : nouvelle alerte présentée immédiatement.
        SetAcAndSettle(true);
        Assert.Equal(AlertState.AlertHigh, State);
        Assert.True(Engine.Output.ShouldPresent);
    }

    [Fact]
    public void Snooze_has_no_effect_when_not_in_alert()
    {
        Start(level: 50, ac: false);

        Engine.Snooze();

        Assert.Null(Engine.Output.SnoozeUntil);
        Assert.Empty(Log.OfKind(LogEventKind.Snooze));
    }

    [Fact]
    public void Snooze_has_no_effect_without_battery()
    {
        Start(level: 0, ac: true, hasBattery: false);

        Engine.Snooze();

        Assert.Null(Engine.Output.SnoozeUntil);
    }

    [Fact]
    public void Snooze_is_logged()
    {
        Start(level: 15, ac: false);

        Engine.Snooze();

        var entry = Assert.Single(Log.OfKind(LogEventKind.Snooze));
        Assert.Contains("15 min", entry.Detail);
    }
}
