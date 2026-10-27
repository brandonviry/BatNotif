using BatNotif.Core.Alerts;

namespace BatNotif.Core.Battery;

/// <summary>
/// Filtre la présence du secteur : un changement n'est retenu que s'il dure au moins 2 s (FR-003),
/// pour ignorer les micro-coupures. Thread-safe.
/// </summary>
public sealed class AcPowerDebouncer : IDisposable
{
    private readonly TimeProvider _time;
    private readonly Action<bool> _onStableChanged;
    private readonly TimeSpan _delay;
    private readonly Lock _gate = new();
    private ITimer? _pending;
    private bool? _pendingValue;
    private bool _stable;

    public AcPowerDebouncer(TimeProvider time, bool initial, Action<bool> onStableChanged, TimeSpan? delay = null)
    {
        _time = time;
        _stable = initial;
        _onStableChanged = onStableChanged;
        _delay = delay ?? AlertConstants.AcDebounce;
    }

    /// <summary>Valeur retenue après filtrage.</summary>
    public bool Stable
    {
        get
        {
            lock (_gate)
            {
                return _stable;
            }
        }
    }

    /// <summary>Signale la valeur brute lue sur le système.</summary>
    public void Observe(bool raw)
    {
        lock (_gate)
        {
            if (raw == _stable)
            {
                CancelPending();
                return;
            }

            if (_pendingValue == raw)
            {
                return; // Délai déjà en cours pour cette valeur.
            }

            CancelPending();
            _pendingValue = raw;
            _pending = _time.CreateTimer(_ => Commit(raw), null, _delay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>Impose une valeur immédiatement, sans délai ni notification (ex. sortie de veille).</summary>
    public void Reset(bool value)
    {
        lock (_gate)
        {
            CancelPending();
            _stable = value;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            CancelPending();
        }
    }

    private void Commit(bool value)
    {
        lock (_gate)
        {
            if (_pendingValue != value)
            {
                return; // Annulé entre-temps.
            }

            CancelPending();
            _stable = value;
        }

        _onStableChanged(value);
    }

    private void CancelPending()
    {
        _pending?.Dispose();
        _pending = null;
        _pendingValue = null;
    }
}
