using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using BatNotif.App.Logging;
using BatNotif.App.Platform;
using BatNotif.Core.Logging;

namespace BatNotif.App;

/// <summary>
/// Point d'entrée. L'application vit dans la zone de notification : aucune fenêtre au démarrage,
/// arrêt uniquement sur demande explicite (ShutdownMode OnExplicitShutdown).
/// </summary>
public partial class App : Application
{
    private SingleInstance? _singleInstance;

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
    }

    protected override void OnExit(ExitEventArgs e)
    {
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
