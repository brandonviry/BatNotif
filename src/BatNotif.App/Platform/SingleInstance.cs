using System.Threading;

namespace BatNotif.App.Platform;

/// <summary>
/// Garantit une seule instance par session utilisateur (constitution II, FR-032).
/// Un second lancement réveille l'instance existante puis se termine.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Local\BatNotif.SingleInstance";
    private const string ActivationEventName = @"Local\BatNotif.Activate";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activation;
    private readonly RegisteredWaitHandle? _registration;

    private SingleInstance(Mutex mutex, EventWaitHandle activation, bool isFirst)
    {
        _mutex = mutex;
        _activation = activation;
        IsFirstInstance = isFirst;
        if (isFirst)
        {
            _registration = ThreadPool.RegisterWaitForSingleObject(
                _activation,
                (_, _) => ActivationRequested?.Invoke(this, EventArgs.Empty),
                state: null,
                Timeout.Infinite,
                executeOnlyOnce: false);
        }
    }

    public bool IsFirstInstance { get; }

    /// <summary>
    /// Levé (sur un thread du pool) quand un second lancement demande d'afficher la fenêtre.
    /// </summary>
    public event EventHandler? ActivationRequested;

    /// <summary>
    /// Tente de devenir l'instance principale. Si une instance existe déjà, la signale.
    /// </summary>
    public static SingleInstance Acquire()
    {
        var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        var activation = new EventWaitHandle(false, EventResetMode.AutoReset, ActivationEventName);
        if (!createdNew)
        {
            activation.Set();
        }

        return new SingleInstance(mutex, activation, createdNew);
    }

    public void Dispose()
    {
        _registration?.Unregister(null);
        if (IsFirstInstance)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Mutex déjà relâché ou possédé par un autre thread : rien à faire à l'arrêt.
            }
        }

        _mutex.Dispose();
        _activation.Dispose();
    }
}
