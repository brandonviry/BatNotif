using BatNotif.Core.Alerts;
using BatNotif.Core.Battery;
using BatNotif.Core.Settings;
using Microsoft.Extensions.Time.Testing;

namespace BatNotif.Core.Tests.Foundation;

public class CoreTypesTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(0, 0)]
    [InlineData(64, 64)]
    [InlineData(100, 100)]
    [InlineData(130, 100)]
    public void Snapshot_level_is_clamped_to_0_100(int raw, int expected)
    {
        var snapshot = new BatterySnapshot(raw, true, false, false, true, T0);

        Assert.Equal(expected, snapshot.Level);
    }

    [Theory]
    [InlineData(false, false, true)]  // sur batterie
    [InlineData(true, true, true)]    // secteur mais décharge (chargeur trop faible)
    [InlineData(true, false, false)]  // secteur, pas de décharge
    public void Effectively_on_battery_follows_ac_and_discharge(bool ac, bool discharging, bool expected)
    {
        var snapshot = new BatterySnapshot(50, true, ac, !discharging && ac, discharging, T0);

        Assert.Equal(expected, snapshot.EffectivelyOnBattery);
    }

    [Fact]
    public void SameStateAs_ignores_observation_time()
    {
        var a = new BatterySnapshot(50, true, true, true, false, T0);
        var b = a with { ObservedAt = T0.AddMinutes(3) };

        Assert.True(a.SameStateAs(b));
        Assert.False(a.SameStateAs(b with { Level = 51 }));
    }

    [Fact]
    public void Timed_pause_is_active_until_its_end()
    {
        var time = new FakeTimeProvider(T0);
        var pause = PauseState.For(time, TimeSpan.FromMinutes(15));

        Assert.True(pause.IsActiveAt(time.GetUtcNow()));
        time.Advance(TimeSpan.FromMinutes(15));
        Assert.False(pause.IsActiveAt(time.GetUtcNow()));
    }

    [Fact]
    public void Indefinite_and_none_pauses()
    {
        Assert.True(PauseState.Indefinite.IsActiveAt(T0.AddYears(1)));
        Assert.False(PauseState.None.IsActiveAt(T0));
    }

    [Fact]
    public void Default_settings_match_data_model()
    {
        var settings = AppSettings.Default;

        Assert.Equal(20, settings.LowThreshold);
        Assert.Equal(80, settings.HighThreshold);
        Assert.Equal(22, settings.LowReset);
        Assert.Equal(78, settings.HighReset);
        Assert.Equal(0.8, settings.Volume);
        Assert.False(settings.StartWithWindows);
        Assert.Equal(PauseKind.None, settings.Pause.Kind);
        Assert.Equal(5, AlertConstants.MinThresholdGap);
    }
}
