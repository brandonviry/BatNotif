using BatNotif.Core.Alerts;
using BatNotif.Core.Logging;
using BatNotif.Core.Settings;

namespace BatNotif.Core.Tests.Alerts;

/// <summary>FR-023 à FR-026 : pause 15 min / 1 h / indéfinie, reprise, persistance.</summary>
public class AlertEnginePauseTests : EngineTestBase
{
    [Theory]
    [InlineData(15)]
    [InlineData(60)]
    public void Timed_pause_silences_alerts_then_reevaluates_at_its_end(int minutes)
    {
        Start(level: 85, ac: true);

        Engine.Pause(PauseState.For(Time, TimeSpan.FromMinutes(minutes)));

        Assert.False(Engine.Output.ShouldPresent);
        Assert.True(Engine.Output.IsPaused);
        Assert.Equal(Time.GetUtcNow().AddMinutes(minutes), Engine.Output.PauseUntil);

        Time.Advance(TimeSpan.FromMinutes(minutes));

        Assert.False(Engine.Output.IsPaused);
        Assert.True(Engine.Output.ShouldPresent); // Alerte immédiate à la fin de la pause.
    }

    [Fact]
    public void Indefinite_pause_lasts_until_resume()
    {
        Start(level: 85, ac: true);

        Engine.Pause(PauseState.Indefinite);
        Time.Advance(TimeSpan.FromDays(3));

        Assert.True(Engine.Output.PauseIndefinite);
        Assert.False(Engine.Output.ShouldPresent);
    }

    [Fact]
    public void Real_state_keeps_being_tracked_during_pause()
    {
        Start(level: 70, ac: true);
        Engine.Pause(PauseState.Indefinite);

        Battery.SetLevel(85);

        Assert.Equal(AlertState.AlertHigh, State);
        Assert.Equal(85, Engine.Output.Level);
        Assert.False(Engine.Output.ShouldPresent);
    }

    [Fact]
    public void Resume_reevaluates_immediately()
    {
        Start(level: 70, ac: true);
        Engine.Pause(PauseState.Indefinite);
        Battery.SetLevel(85);

        Engine.Resume();

        Assert.False(Engine.Output.IsPaused);
        Assert.True(Engine.Output.ShouldPresent);
    }

    [Fact]
    public void Resume_without_alert_condition_stays_silent()
    {
        Start(level: 50, ac: false);
        Engine.Pause(PauseState.For(Time, TimeSpan.FromHours(1)));

        Engine.Resume();

        Assert.False(Engine.Output.ShouldPresent);
        Assert.Equal(AlertState.Normal, State);
    }

    [Fact]
    public void Pause_changed_is_raised_on_start_resume_and_expiry()
    {
        Start(level: 50, ac: false);

        Engine.Pause(PauseState.Indefinite);
        Engine.Resume();
        Engine.Pause(PauseState.For(Time, TimeSpan.FromMinutes(15)));
        Time.Advance(TimeSpan.FromMinutes(15));

        Assert.Equal(4, PauseChanges.Count);
        Assert.Equal(PauseKind.Indefinite, PauseChanges[0].Kind);
        Assert.Equal(PauseKind.None, PauseChanges[1].Kind);
        Assert.Equal(PauseKind.Timed, PauseChanges[2].Kind);
        Assert.Equal(PauseKind.None, PauseChanges[3].Kind);
    }

    [Fact]
    public void Pause_restored_at_startup_is_applied()
    {
        var until = Time.GetUtcNow().AddMinutes(30);

        Start(level: 85, ac: true, pause: new PauseState(PauseKind.Timed, until));

        Assert.False(Engine.Output.ShouldPresent);
        Assert.Equal(until, Engine.Output.PauseUntil);
        Time.Advance(TimeSpan.FromMinutes(30));
        Assert.True(Engine.Output.ShouldPresent);
    }

    [Fact]
    public void Expired_pause_at_startup_is_ignored()
    {
        Start(level: 85, ac: true, pause: new PauseState(PauseKind.Timed, Time.GetUtcNow().AddMinutes(-1)));

        Assert.False(Engine.Output.IsPaused);
        Assert.True(Engine.Output.ShouldPresent);
    }

    [Fact]
    public void Pause_clears_a_running_snooze()
    {
        Start(level: 85, ac: true);
        Engine.Snooze();

        Engine.Pause(PauseState.Indefinite);
        Engine.Resume();

        Assert.Null(Engine.Output.SnoozeUntil);
        Assert.True(Engine.Output.ShouldPresent);
    }

    [Fact]
    public void Snooze_is_ignored_during_pause()
    {
        Start(level: 85, ac: true);
        Engine.Pause(PauseState.Indefinite);

        Engine.Snooze();

        Assert.Null(Engine.Output.SnoozeUntil);
    }

    [Fact]
    public void Pause_start_and_end_are_logged()
    {
        Start(level: 50, ac: false);

        Engine.Pause(PauseState.For(Time, TimeSpan.FromHours(1)));
        Engine.Resume();

        Assert.Single(Log.OfKind(LogEventKind.PauseStarted));
        Assert.Single(Log.OfKind(LogEventKind.PauseEnded));
    }

    [Fact]
    public void Current_pause_is_exposed_for_persistence()
    {
        Start(level: 50, ac: false);

        Engine.Pause(PauseState.Indefinite);

        Assert.Equal(PauseState.Indefinite, Engine.CurrentPause);
    }
}
