using BatNotif.Core.Battery;
using BatNotif.Core.Logging;

namespace BatNotif.Core.Alerts;

/// <summary>
/// Moteur d'alerte : machine à états secteur × niveau avec hystérésis (data-model §3, engine-contract).
/// Thread-safe ; les événements sont levés hors verrou, sur le thread qui a provoqué le changement.
/// </summary>
public sealed class AlertEngine : IDisposable
{
    private readonly IBatterySource _source;
    private readonly TimeProvider _time;
    private readonly IEventLog _log;
    private readonly AcPowerDebouncer _ac;
    private readonly Lock _gate = new();

    private int _low;
    private int _high;
    private AlertState _state = AlertState.Normal;
    private AlertOutput _output = AlertOutput.Initial;
    private bool _disposed;

    public AlertEngine(IBatterySource source, TimeProvider time, IEventLog log, int lowThreshold, int highThreshold)
    {
        _source = source;
        _time = time;
        _log = log;
        _low = lowThreshold;
        _high = highThreshold;
        _ac = new AcPowerDebouncer(time, source.Current.AcPresent, _ => Recompute());
        _source.SnapshotChanged += OnSnapshotChanged;
    }

    /// <summary>Sortie courante ; ne présente rien avant le premier <see cref="Evaluate"/>.</summary>
    public AlertOutput Output
    {
        get
        {
            lock (_gate)
            {
                return _output;
            }
        }
    }

    /// <summary>Levé à chaque changement de <see cref="Output"/>.</summary>
    public event EventHandler<AlertOutput>? OutputChanged;

    /// <summary>Évalue immédiatement l'état courant (démarrage, FR-004).</summary>
    public void Evaluate() => Guard(() =>
    {
        _ac.Observe(_source.Current.AcPresent);
        Recompute();
    });

    /// <summary>Sortie de veille : relit l'état et l'applique sans debounce (FR-004).</summary>
    public void NotifyResumedFromSleep() => Guard(() =>
    {
        var snapshot = _source.Current;
        _ac.Reset(snapshot.AcPresent);
        _log.Write(_time, LogEventKind.Info, $"Sortie de veille : réévaluation ({snapshot.Level} %, {AcLabel(snapshot.AcPresent)})");
        Recompute();
    });

    /// <summary>Applique de nouveaux seuils (validés en amont) et réévalue immédiatement (US2).</summary>
    public void UpdateThresholds(int lowThreshold, int highThreshold) => Guard(() =>
    {
        lock (_gate)
        {
            if (lowThreshold == _low && highThreshold == _high)
            {
                return;
            }

            _low = lowThreshold;
            _high = highThreshold;
            _log.Write(_time, LogEventKind.Info, $"Nouveaux seuils appliqués : {_low} % / {_high} %");
        }

        Recompute();
    });

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        _source.SnapshotChanged -= OnSnapshotChanged;
        _ac.Dispose();
    }

    private void OnSnapshotChanged(object? sender, BatterySnapshot snapshot) => Guard(() =>
    {
        _ac.Observe(snapshot.AcPresent);
        Recompute();
    });

    private void Recompute()
    {
        AlertOutput? changed = null;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            var snapshot = _source.Current;
            var ac = _ac.Stable;
            var next = NextState(_state, snapshot, ac);
            if (next != _state)
            {
                _log.Write(_time, LogEventKind.StateChanged,
                    $"{_state} → {next} à {snapshot.Level} % ({AcLabel(ac)})");
                _state = next;
            }

            var output = new AlertOutput(
                _state,
                ShouldPresent: _state is AlertState.AlertLow or AlertState.AlertHigh,
                snapshot.Level,
                ac,
                PauseUntil: null,
                PauseIndefinite: false,
                SnoozeUntil: null);

            if (output != _output)
            {
                _output = output;
                changed = output;
            }
        }

        if (changed is not null)
        {
            Raise(changed);
        }
    }

    /// <summary>Règles d'entrée / sortie d'alerte avec hystérésis (FR-006 à FR-009).</summary>
    private AlertState NextState(AlertState current, BatterySnapshot s, bool ac)
    {
        if (!s.HasBattery)
        {
            return AlertState.NoBattery;
        }

        var lowReset = _low + AlertConstants.Hysteresis;
        var highReset = _high - AlertConstants.Hysteresis;
        var onBattery = !ac || s.Discharging;

        // Une alerte en cours ne se termine qu'au franchissement de son seuil de fin.
        if (current == AlertState.AlertLow && !(s.Level >= lowReset || (ac && s.Charging)))
        {
            return AlertState.AlertLow;
        }

        if (current == AlertState.AlertHigh && !(s.Level <= highReset || !ac))
        {
            return AlertState.AlertHigh;
        }

        if (onBattery && s.Level <= _low)
        {
            return AlertState.AlertLow;
        }

        if (ac && s.Level >= _high)
        {
            return AlertState.AlertHigh;
        }

        return AlertState.Normal;
    }

    private void Raise(AlertOutput output)
    {
        var handlers = OutputChanged;
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<EventHandler<AlertOutput>>())
        {
            try
            {
                handler(this, output);
            }
            catch (Exception ex)
            {
                _log.Write(_time, LogEventKind.Error, $"Abonné à OutputChanged en échec — {ex}");
            }
        }
    }

    /// <summary>Aucune exception ne s'échappe du moteur (engine-contract, garantie 8).</summary>
    private void Guard(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            _log.Write(_time, LogEventKind.Error, $"Erreur du moteur d'alerte — {ex}");
        }
    }

    private static string AcLabel(bool ac) => ac ? "secteur présent" : "sur batterie";
}
