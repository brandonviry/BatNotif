using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using BatNotif.App.Audio;
using BatNotif.App.Logging;
using BatNotif.App.Platform;
using BatNotif.App.Presentation;
using BatNotif.App.Tray;
using BatNotif.Core.Alerts;
using BatNotif.Core.Logging;
using BatNotif.Core.Settings;

namespace BatNotif.App;

/// <summary>
/// Point d'entrée. L'application vit dans la zone de notification : aucune fenêtre au démarrage,
/// arrêt uniquement sur demande explicite (ShutdownMode OnExplicitShutdown).
/// </summary>
public partial class App : Application
{
    private SingleInstance? _singleInstance;
    private WindowsBatterySource? _battery;
    private AlertEngine? _engine;
    private LoopingAlarmPlayer? _player;
    private AlertPresenter? _presenter;
    private SleepResumeWatcher? _sleep;
    private TrayIconService? _tray;

    /// <summary>Journal de l'application, disponible dès le début de <see cref="OnStartup"/>.</summary>
    public static IEventLog Log { get; private set; } = new FileEventLog();

    public static TimeProvider Time { get; } = TimeProvider.System;

    /// <summary>
    /// Levé quand une erreur inattendue peut empêcher les alertes (FR-035). Message en français,
    /// destiné aux avertissements de la fenêtre principale.
    /// </summary>
    public static event EventHandler<string>? CriticalErrorReported;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Instance unique en tout premier (constitution II, FR-032).
        _singleInstance = SingleInstance.Acquire();
        if (!_singleInstance.IsFirstInstance)
        {
            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown(0);
            return;
        }

        RegisterGlobalExceptionHandlers();
        base.OnStartup(e);

        Log.Write(Time, LogEventKind.AppStart, $"BatNotif {typeof(App).Assembly.GetName().Version} démarré");

        // Tant que la fenêtre principale n'existe pas (US2), une demande d'activation est seulement journalisée.
        _singleInstance.ActivationRequested += (_, _) =>
            Log.Write(Time, LogEventKind.Info, "Second lancement détecté : activation demandée");

        StartMonitoring(e.Args);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _presenter?.Dispose();
        _engine?.Dispose();
        _sleep?.Dispose();
        _battery?.Dispose();
        _player?.Dispose();
        _tray?.Dispose();
        Log.Write(Time, LogEventKind.AppStop, $"BatNotif arrêté (code {e.ApplicationExitCode})");
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    /// <summary>Signale une erreur critique : journal + avertissement utilisateur.</summary>
    public static void ReportCriticalError(string userMessage, Exception? exception = null)
    {
        Log.Write(Time, LogEventKind.Error, exception is null ? userMessage : $"{userMessage} — {exception}");
        CriticalErrorReported?.Invoke(null, userMessage);
    }

    /// <summary>Câble source batterie, moteur, audio, notification, veille et tray, puis évalue immédiatement.</summary>
    private void StartMonitoring(string[] args)
    {
        var settings = AppSettings.Default;
        var low = settings.LowThreshold;
        var high = settings.HighThreshold;
#if DEBUG
        (low, high) = ApplyDebugThresholdOverrides(args, low, high);
#endif

        _battery = new WindowsBatterySource(Time);
        var initial = _battery.Current;
        Log.Write(Time, LogEventKind.Info,
            initial.HasBattery
                ? $"Batterie détectée : {initial.Level} %, {(initial.AcPresent ? "secteur présent" : "sur batterie")}, seuils {low} % / {high} %"
                : "Aucune batterie détectée : aucune alerte ne sera émise");

        _engine = new AlertEngine(_battery, Time, Log, low, high);
        _player = new LoopingAlarmPlayer(Log, Time) { Volume = (float)settings.Volume };
        _presenter = new AlertPresenter(
            _engine,
            _player,
            sound => sound == AlertSound.Low ? settings.LowSoundPath : settings.HighSoundPath,
            Log,
            Time,
            Dispatcher);
        _sleep = new SleepResumeWatcher(Time);
        _sleep.Resumed += (_, source) =>
        {
            Log.Write(Time, LogEventKind.Info, $"Sortie de veille détectée ({source})");
            _engine.NotifyResumedFromSleep();
        };
        _tray = new TrayIconService(quit: () => Shutdown(0));

        // Évaluation immédiate au démarrage (FR-004).
        _engine.Evaluate();
    }

#if DEBUG
    /// <summary>
    /// Build Debug uniquement : <c>--low N</c> / <c>--high N</c> remplacent les seuils pour la session,
    /// sans les enregistrer, pour simuler une alerte sans modifier le code (quickstart).
    /// </summary>
    private static (int Low, int High) ApplyDebugThresholdOverrides(string[] args, int low, int high)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (!int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                continue;
            }

            value = Math.Clamp(value, 0, 100);
            switch (args[i])
            {
                case "--low":
                    low = value;
                    break;
                case "--high":
                    high = value;
                    break;
            }
        }

        if (low != AppSettings.Default.LowThreshold || high != AppSettings.Default.HighThreshold)
        {
            Log.Write(Time, LogEventKind.Info, $"[Debug] Seuils remplacés pour cette session : bas {low} %, haut {high} % (non enregistrés)");
        }

        return (low, high);
    }
#endif

    private void RegisterGlobalExceptionHandlers()
    {
        // Aucune erreur inattendue ne doit fermer l'application silencieusement (FR-035).
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            ReportCriticalError(
                "Erreur inattendue : les alertes peuvent être interrompues. Redémarre BatNotif.",
                args.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Write(Time, LogEventKind.Error, $"Tâche en échec non observée — {args.Exception}");
            args.SetObserved();
        };
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
        ReportCriticalError("Erreur inattendue dans l'interface ; BatNotif continue de surveiller la batterie.", args.Exception);
        args.Handled = true;
    }
}
