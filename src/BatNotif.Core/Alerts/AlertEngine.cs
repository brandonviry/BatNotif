using BatNotif.Core.Battery;
using BatNotif.Core.Logging;
using BatNotif.Core.Settings;

namespace BatNotif.Core.Alerts;

/// <summary>
/// Moteur d'alerte : machine à états secteur × niveau avec hystérésis, plus snooze et pause
/// (data-model §3, engine-contract). Thread-safe ; les événements sont levés hors verrou,
/// sur le thread qui a provoqué le changement.
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
    private PauseState _pause = PauseState.None;
    private ITimer? _pauseTimer;
    private DateTimeOffset? _snoozeUntil;
    private ITimer? _snoozeTimer;
    private bool _disposed;

    public AlertEngine(
        IBatterySource source, TimeProvider time, IEventLog log, int lowThreshold, int highThreshold,
        PauseState? initialPause = null)
    {
        _source = source;
        _time = time;
        _log = log;
        _low = lowThreshold;
        _high = highThreshold;
        _ac = new AcPowerDebouncer(time, source.Current.AcPresent, _ => Recompute());

        // Pause restaurée au démarrage (FR-026) ; une pause expirée est ignorée.
        if (initialPause is not null && initialPause.IsActiveAt(time.GetUtcNow()))
        {
            _pause = initialPause;
            SchedulePauseEnd();
            _log.Write(_time, LogEventKind.Info, $"Pause restaurée : {Describe(_pause)}");
        }

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

    /// <summary>Pause en vigueur, à persister dans les réglages.</summary>
    public PauseState CurrentPause
    {
        get
        {
            lock (_gate)
            {
                return _pause;
            }
        }
    }

    /// <summary>Levé à chaque changement de <see cref="Output"/>.</summary>
    public event EventHandler<AlertOutput>? OutputChanged;

    /// <summary>Levé à chaque changement de pause (début, reprise, échéance), pour la persister (FR-026).</summary>
    public event EventHandler<PauseState>? PauseChanged;

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

    /// <summary>
    /// Fait taire l'alerte en cours pendant 15 min (FR-027). Sans effet hors alerte ou pendant une pause.
    /// </summary>
    public void Snooze() => Guard(() =>
    {
        lock (_gate)
        {
            if (_state is not (AlertState.AlertLow or AlertState.AlertHigh) || _pause.Kind != PauseKind.None)
            {
                return;
            }

            CancelSnooze();
            _snoozeUntil = _time.GetUtcNow() + AlertConstants.SnoozeDuration;
            _snoozeTimer = _time.CreateTimer(_ => EndSnooze(), null, AlertConstants.SnoozeDuration, Timeout.InfiniteTimeSpan);
            _log.Write(_time, LogEventKind.Snooze, $"{_state} mise en sourdine pour {AlertConstants.SnoozeDuration.TotalMinutes:0} min");
        }

        Recompute();
    });

    /// <summary>Met en pause : aucune présentation, l'état réel reste suivi (FR-023, FR-024).</summary>
    public void Pause(PauseState pause) => Guard(() =>
    {
        if (pause.Kind == PauseKind.None)
        {
            Resume();
            return;
        }

        lock (_gate)
        {
            CancelSnooze();
            CancelPauseTimer();
            _pause = pause;
            SchedulePauseEnd();
            _log.Write(_time, LogEventKind.PauseStarted, $"Pause {Describe(pause)}");
        }

        RaisePauseChanged(pause);
        Recompute();
    });

    /// <summary>Termine la pause et réévalue immédiatement (FR-025).</summary>
    public void Resume() => Guard(() => EndPause("reprise demandée par l'utilisateur"));

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            CancelSnooze();
            CancelPauseTimer();
        }

        _source.SnapshotChanged -= OnSnapshotChanged;
        _ac.Dispose();
    }

    private void OnSnapshotChanged(object? sender, BatterySnapshot snapshot) => Guard(() =>
    {
        _ac.Observe(snapshot.AcPresent);
        Recompute();
    });

    private void EndSnooze() => Guard(() =>
    {
        lock (_gate)
        {
            CancelSnooze();
        }

        _log.Write(_time, LogEventKind.Info, "Fin du snooze : réévaluation");
        Recompute();
    });

    private void EndPause(string reason)
    {
        lock (_gate)
        {
            if (_pause.Kind == PauseKind.None)
            {
                return;
            }

            CancelPauseTimer();
            _pause = PauseState.None;
            _log.Write(_time, LogEventKind.PauseEnded, $"Fin de pause : {reason}");
        }

        RaisePauseChanged(PauseState.None);
        Recompute();
    }

    private void SchedulePauseEnd()
    {
        if (_pause is { Kind: PauseKind.Timed, Until: { } until })
        {
            var delay = until - _time.GetUtcNow();
            _pauseTimer = _time.CreateTimer(
                _ => Guard(() => EndPause("échéance atteinte")),
                null,
                delay > TimeSpan.Zero ? delay : TimeSpan.Zero,
                Timeout.InfiniteTimeSpan);
        }
    }

    private void CancelSnooze()
    {
        _snoozeTimer?.Dispose();
        _snoozeTimer = null;
        _snoozeUntil = null;
    }

    private void CancelPauseTimer()
    {
        _pauseTimer?.Dispose();
        _pauseTimer = null;
    }

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

            var inAlert = _state is AlertState.AlertLow or AlertState.AlertHigh;
            if (!inAlert && _snoozeUntil is not null)
            {
                CancelSnooze(); // Le snooze ne vaut que pour l'alerte en cours (FR-027).
            }

            var paused = _pause.Kind != PauseKind.None;
            var output = new AlertOutput(
                _state,
                ShouldPresent: inAlert && !paused && _snoozeUntil is null,
                snapshot.Level,
                ac,
                PauseUntil: _pause.Kind == PauseKind.Timed ? _pause.Until : null,
                PauseIndefinite: _pause.Kind == PauseKind.Indefinite,
                SnoozeUntil: _snoozeUntil);

            if (output != _output)
            {
                _output = output;
                changed = output;
            }
        }

        if (changed is not null)
        {
            Raise(OutputChanged, changed);
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

    private void RaisePauseChanged(PauseState pause) => Raise(PauseChanged, pause);

    private void Raise<T>(EventHandler<T>? handlers, T value)
    {
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<EventHandler<T>>())
        {
            try
            {
                handler(this, value);
            }
            catch (Exception ex)
            {
                _log.Write(_time, LogEventKind.Error, $"Abonné à un événement du moteur en échec — {ex}");
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

    private static string Describe(PauseState pause) => pause.Kind switch
    {
        PauseKind.Indefinite => "indéfinie",
        PauseKind.Timed => $"jusqu'à {pause.Until?.ToLocalTime():HH:mm}",
        _ => "aucune",
    };

    private static string AcLabel(bool ac) => ac ? "secteur présent" : "sur batterie";
}
