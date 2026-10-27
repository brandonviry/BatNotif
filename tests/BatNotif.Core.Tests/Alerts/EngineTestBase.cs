using BatNotif.Core.Alerts;
using BatNotif.Core.Tests.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace BatNotif.Core.Tests.Alerts;

/// <summary>Montage commun : moteur réel + batterie, horloge et journal simulés.</summary>
public abstract class EngineTestBase : IDisposable
{
    protected static readonly TimeSpan Debounce = AlertConstants.AcDebounce;

    private AlertEngine? _engine;

    protected FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero));

    protected InMemoryEventLog Log { get; } = new();

    protected FakeBatterySource Battery { get; private set; } = null!;

    protected AlertEngine Engine => _engine ?? throw new InvalidOperationException("Appeler Start() d'abord.");

    protected List<AlertOutput> Outputs { get; } = [];

    /// <summary>Crée le moteur sur un état initial puis lance l'évaluation de démarrage.</summary>
    protected AlertEngine Start(int level, bool ac, int low = 20, int high = 80, bool hasBattery = true)
    {
        Battery = new FakeBatterySource(Time, level, ac, hasBattery);
        _engine = new AlertEngine(Battery, Time, Log, low, high);
        _engine.OutputChanged += (_, output) => Outputs.Add(output);
        _engine.Evaluate();
        return _engine;
    }

    /// <summary>Change le secteur puis laisse passer le debounce.</summary>
    protected void SetAcAndSettle(bool ac)
    {
        Battery.SetAc(ac);
        Time.Advance(Debounce);
    }

    protected AlertState State => Engine.Output.State;

    public void Dispose()
    {
        _engine?.Dispose();
        GC.SuppressFinalize(this);
    }
}
