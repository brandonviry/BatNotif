using BatNotif.Core.Alerts;
using BatNotif.Core.Battery;
using BatNotif.Core.Logging;

namespace BatNotif.Core.Tests.Alerts;

/// <summary>Alerte continue (FR-010), évaluation immédiate au démarrage et au réveil (FR-004), robustesse.</summary>
public class AlertEngineContinuityTests : EngineTestBase
{
    [Fact]
    public void Alert_keeps_presenting_for_hours_while_condition_holds()
    {
        Start(level: 90, ac: true);

        for (var hour = 0; hour < 6; hour++)
        {
            Time.Advance(TimeSpan.FromHours(1));
            Assert.Equal(AlertState.AlertHigh, State);
            Assert.True(Engine.Output.ShouldPresent);
        }
    }

    [Fact]
    public void Startup_plugged_at_90_alerts_immediately()
    {
        Start(level: 90, ac: true);

        Assert.Equal(AlertState.AlertHigh, State);
        Assert.True(Engine.Output.ShouldPresent);
        Assert.Contains(Outputs, o => o.ShouldPresent);
    }

    [Fact]
    public void Startup_on_battery_at_90_does_not_alert()
    {
        Start(level: 90, ac: false);

        Assert.Equal(AlertState.Normal, State);
        Assert.DoesNotContain(Outputs, o => o.ShouldPresent);
    }

    [Fact]
    public void Output_before_first_evaluation_does_not_present()
    {
        var battery = new Fakes.FakeBatterySource(Time, level: 90, acPresent: true);
        using var engine = new AlertEngine(battery, Time, Log, 20, 80);

        Assert.False(engine.Output.ShouldPresent);
    }

    [Fact]
    public void Resume_from_sleep_reevaluates_immediately_without_debounce()
    {
        Start(level: 70, ac: false);

        // Pendant la veille, le PC a été branché et a chargé : aucun événement n'a été reçu.
        Battery.RaiseRawSilently(new BatterySnapshot(85, true, true, true, false, Time.GetUtcNow()));
        Assert.Equal(AlertState.Normal, State);

        Engine.NotifyResumedFromSleep();

        Assert.Equal(AlertState.AlertHigh, State);
        Assert.True(Engine.Output.ShouldPresent);
    }

    [Fact]
    public void Output_changed_is_raised_only_on_actual_change()
    {
        Start(level: 50, ac: false);
        var count = Outputs.Count;

        Battery.Push(level: 50, acPresent: false);
        Engine.Evaluate();

        Assert.Equal(count, Outputs.Count);

        Battery.SetLevel(49);
        Assert.Equal(count + 1, Outputs.Count);
    }

    [Fact]
    public void Level_changes_during_alert_update_the_output()
    {
        Start(level: 85, ac: true);

        Battery.SetLevel(86);

        Assert.Equal(86, Engine.Output.Level);
        Assert.Equal(86, Outputs[^1].Level);
    }

    [Fact]
    public void Faulty_snapshot_is_logged_as_error_without_throwing()
    {
        Start(level: 50, ac: false);

        var exception = Record.Exception(() => Battery.RaiseRaw(null!));

        Assert.Null(exception);
        Assert.Single(Log.OfKind(LogEventKind.Error));
        Assert.Equal(AlertState.Normal, State);
    }

    [Fact]
    public void Faulty_subscriber_does_not_break_the_engine()
    {
        Start(level: 79, ac: true);
        Engine.OutputChanged += (_, _) => throw new InvalidOperationException("abonné en panne");

        var exception = Record.Exception(() => Battery.SetLevel(80));

        Assert.Null(exception);
        Assert.Equal(AlertState.AlertHigh, State);
        Assert.NotEmpty(Log.OfKind(LogEventKind.Error));
    }
}
