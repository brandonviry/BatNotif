using BatNotif.Core.Battery;

namespace BatNotif.Core.Tests.Fakes;

/// <summary>Source batterie pilotée par les tests.</summary>
internal sealed class FakeBatterySource : IBatterySource
{
    private readonly TimeProvider _time;

    public FakeBatterySource(TimeProvider time, int level = 50, bool acPresent = false, bool hasBattery = true)
    {
        _time = time;
        Current = Make(level, acPresent, hasBattery);
    }

    public BatterySnapshot Current { get; private set; }

    public event EventHandler<BatterySnapshot>? SnapshotChanged;

    /// <summary>Remplace l'état courant et lève <see cref="SnapshotChanged"/>.</summary>
    public void Push(int level, bool acPresent, bool? charging = null, bool? discharging = null, bool hasBattery = true)
    {
        Current = Make(level, acPresent, hasBattery, charging, discharging);
        SnapshotChanged?.Invoke(this, Current);
    }

    /// <summary>Change seulement le niveau, en gardant l'état du secteur.</summary>
    public void SetLevel(int level) => Push(level, Current.AcPresent);

    /// <summary>Change seulement le secteur, en gardant le niveau.</summary>
    public void SetAc(bool acPresent) => Push(Current.Level, acPresent);

    /// <summary>Lève l'événement avec un instantané qui fait échouer le consommateur.</summary>
    public void RaiseRaw(BatterySnapshot snapshot)
    {
        Current = snapshot;
        SnapshotChanged?.Invoke(this, snapshot);
    }

    private BatterySnapshot Make(
        int level, bool acPresent, bool hasBattery, bool? charging = null, bool? discharging = null) =>
        new(
            level,
            hasBattery,
            acPresent,
            Charging: charging ?? (acPresent && level < 100),
            Discharging: discharging ?? !acPresent,
            _time.GetUtcNow());
}
