using System.Windows.Threading;
using BatNotif.App.Audio;
using BatNotif.App.Platform;
using BatNotif.App.ViewModels;
using BatNotif.App.Views;
using BatNotif.Core.Alerts;
using BatNotif.Core.Logging;

namespace BatNotif.App.Presentation;

/// <summary>
/// Traduit la sortie du moteur en notification + son en boucle (FR-010, FR-011, FR-015) et gère
/// l'alerte renforcée en plein écran (FR-037). Tout s'exécute sur le Dispatcher UI.
/// </summary>
/// <remarks>
/// Le mode renforcé est « collant » pendant une présentation : une fois l'alerte passée devant une application
/// plein écran, elle reste agrandie jusqu'à la fin de l'alerte, du snooze ou de la pause. Si l'utilisateur retourne
/// en plein écran sans corriger la situation, l'alerte repasse devant (alerte continue).
/// </remarks>
public sealed class AlertPresenter : IDisposable
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);

    private readonly AlertEngine _engine;
    private readonly LoopingAlarmPlayer _player;
    private readonly Func<AlertSound, string> _soundPathFor;
    private readonly IEventLog _log;
    private readonly TimeProvider _time;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _checkTimer;
    private readonly NotificationViewModel _viewModel;
    private NotificationWindow? _window;
    private AlertSound? _presented;

    public AlertPresenter(
        AlertEngine engine,
        LoopingAlarmPlayer player,
        Func<AlertSound, string> soundPathFor,
        IEventLog log,
        TimeProvider time,
        Dispatcher dispatcher)
    {
        _engine = engine;
        _player = player;
        _soundPathFor = soundPathFor;
        _log = log;
        _time = time;
        _dispatcher = dispatcher;
        _viewModel = new NotificationViewModel { SnoozeCommand = new RelayCommand(_engine.Snooze) };
        _checkTimer = new DispatcherTimer(CheckInterval, DispatcherPriority.Normal, (_, _) => CheckPresentation(), dispatcher);
        _checkTimer.Stop();
        _engine.OutputChanged += OnOutputChanged;
    }

    public void Dispose()
    {
        _engine.OutputChanged -= OnOutputChanged;
        _checkTimer.Stop();
        Hide();
        _window?.Close();
        _window = null;
    }

    private void OnOutputChanged(object? sender, AlertOutput output) =>
        _dispatcher.BeginInvoke(() => Apply(output));

    private void Apply(AlertOutput output)
    {
        if (!output.ShouldPresent)
        {
            Hide();
            return;
        }

        var sound = output.State == AlertState.AlertLow ? AlertSound.Low : AlertSound.High;
        _viewModel.Level = output.Level;
        if (_presented == sound)
        {
            return; // Déjà affichée : seul le niveau change.
        }

        _viewModel.Sound = sound;
        _window ??= new NotificationWindow(_viewModel);
        _window.SetMode(NotificationMode.Normal);
        _window.Present();
        _player.Play(sound, _soundPathFor(sound));
        _presented = sound;
        _log.Write(_time, LogEventKind.AlertPresented, $"{_viewModel.Title} : {_viewModel.Message}");

        CheckPresentation();
        _checkTimer.Start();
    }

    private void Hide()
    {
        if (_presented is null)
        {
            return;
        }

        _checkTimer.Stop();
        _player.Stop();
        _window?.Hide();
        _window?.SetMode(NotificationMode.Normal);
        _presented = null;
    }

    /// <summary>Toutes les 5 s pendant une alerte : plein écran → mode renforcé et premier plan forcé.</summary>
    private void CheckPresentation()
    {
        if (_window is null || _presented is null)
        {
            return;
        }

        if (FullscreenDetector.IsFullscreenActive())
        {
            if (_viewModel.Mode != NotificationMode.Enhanced)
            {
                _window.SetMode(NotificationMode.Enhanced);
            }

            if (!ForegroundHelper.ForceForeground(_window))
            {
                _log.Write(_time, LogEventKind.AlertPresentFailed,
                    "Plein écran : Windows a refusé le premier plan ; la notification reste au-dessus et le son continue");
            }
        }

        _window.ReassertTopmost();
    }
}
