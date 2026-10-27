using BatNotif.Core.Alerts;
using Microsoft.Win32;

namespace BatNotif.App.Platform;

/// <summary>
/// Détecte la sortie de veille / hibernation (research R4, FR-004) : événement système
/// <see cref="SystemEvents.PowerModeChanged"/> + filet de sécurité par saut d'horloge (> 30 s entre deux ticks de 5 s).
/// <see cref="Resumed"/> peut être levé depuis n'importe quel thread.
/// </summary>
public sealed class SleepResumeWatcher : IDisposable
{
    private static readonly TimeSpan JumpThreshold = TimeSpan.FromSeconds(30);

    private readonly TimeProvider _time;
    private readonly ITimer _timer;
    private DateTimeOffset _lastTick;

    public SleepResumeWatcher(TimeProvider time)
    {
        _time = time;
        _lastTick = time.GetUtcNow();
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        _timer = time.CreateTimer(_ => OnTick(), null, AlertConstants.PollingInterval, AlertConstants.PollingInterval);
    }

    /// <summary>Levé après une sortie de veille ; le paramètre indique la source de la détection.</summary>
    public event EventHandler<string>? Resumed;

    public void Dispose()
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _timer.Dispose();
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            _lastTick = _time.GetUtcNow();
            Resumed?.Invoke(this, "événement système");
        }
    }

    private void OnTick()
    {
        var now = _time.GetUtcNow();
        var elapsed = now - _lastTick;
        _lastTick = now;
        if (elapsed > JumpThreshold)
        {
            Resumed?.Invoke(this, $"saut d'horloge de {elapsed.TotalSeconds:F0} s");
        }
    }
}
