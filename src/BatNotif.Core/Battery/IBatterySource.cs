namespace BatNotif.Core.Battery;

/// <summary>
/// Source de l'état batterie : Windows en production, simulée dans les tests (engine-contract, « Entrées »).
/// </summary>
public interface IBatterySource
{
    /// <summary>Dernier instantané connu.</summary>
    BatterySnapshot Current { get; }

    /// <summary>Levé à chaque changement d'état (niveau, secteur, charge, présence de batterie).</summary>
    event EventHandler<BatterySnapshot>? SnapshotChanged;
}
