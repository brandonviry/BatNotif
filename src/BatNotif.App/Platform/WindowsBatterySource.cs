using BatNotif.Core.Alerts;
using BatNotif.Core.Battery;
using Windows.System.Power;

namespace BatNotif.App.Platform;

/// <summary>
/// Source batterie Windows : <see cref="PowerManager"/> (WinRT) et ses événements, plus un polling
/// de secours toutes les 5 s (research R3, FR-001, FR-002, FR-005).
/// <see cref="SnapshotChanged"/> peut être levé depuis n'importe quel thread.
/// </summary>
public sealed class WindowsBatterySource : IBatterySource, IDisposable
{
    private readonly TimeProvider _time;
    private readonly ITimer _pollTimer;
    private readonly Lock _gate = new();
    private BatterySnapshot _current;

    public WindowsBatterySource(TimeProvider time)
    {
        _time = time;
        _current = Read();

        PowerManager.RemainingChargePercentChanged += OnPowerChanged;
        PowerManager.BatteryStatusChanged += OnPowerChanged;
        PowerManager.PowerSupplyStatusChanged += OnPowerChanged;

        _pollTimer = _time.CreateTimer(
            _ => Refresh(), null, AlertConstants.PollingInterval, AlertConstants.PollingInterval);
    }

    public BatterySnapshot Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    public event EventHandler<BatterySnapshot>? SnapshotChanged;

    /// <summary>Relit l'état et lève <see cref="SnapshotChanged"/> s'il a changé.</summary>
    public void Refresh()
    {
        BatterySnapshot snapshot;
        lock (_gate)
        {
            var read = Read();
            if (read.SameStateAs(_current))
            {
                return;
            }

            _current = read;
            snapshot = read;
        }

        SnapshotChanged?.Invoke(this, snapshot);
    }

    public void Dispose()
    {
        PowerManager.RemainingChargePercentChanged -= OnPowerChanged;
        PowerManager.BatteryStatusChanged -= OnPowerChanged;
        PowerManager.PowerSupplyStatusChanged -= OnPowerChanged;
        _pollTimer.Dispose();
    }

    private void OnPowerChanged(object? sender, object e) => Refresh();

    private BatterySnapshot Read()
    {
        var battery = PowerManager.BatteryStatus;
        var supply = PowerManager.PowerSupplyStatus;
        return new BatterySnapshot(
            Level: PowerManager.RemainingChargePercent,
            HasBattery: battery != BatteryStatus.NotPresent,
            AcPresent: supply != PowerSupplyStatus.NotPresent,
            Charging: battery == BatteryStatus.Charging,
            Discharging: battery == BatteryStatus.Discharging,
            ObservedAt: _time.GetUtcNow());
    }
}
