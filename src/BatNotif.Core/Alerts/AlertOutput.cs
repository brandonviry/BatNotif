namespace BatNotif.Core.Alerts;

/// <summary>
/// Sortie du moteur consommée par l'application (engine-contract, « Sorties »).
/// </summary>
/// <param name="State">État d'alerte calculé, même pendant une pause.</param>
/// <param name="ShouldPresent">Vrai si la notification doit être affichée et le son joué.</param>
/// <param name="Level">Niveau de batterie courant.</param>
/// <param name="AcPresent">Secteur présent (après debounce).</param>
/// <param name="PauseUntil">Fin d'une pause minutée, sinon null.</param>
/// <param name="PauseIndefinite">Vrai si une pause indéfinie est active.</param>
/// <param name="SnoozeUntil">Fin du snooze en cours, sinon null.</param>
public sealed record AlertOutput(
    AlertState State,
    bool ShouldPresent,
    int Level,
    bool AcPresent,
    DateTimeOffset? PauseUntil,
    bool PauseIndefinite,
    DateTimeOffset? SnoozeUntil)
{
    public bool IsPaused => PauseIndefinite || PauseUntil is not null;

    public bool IsSnoozed => SnoozeUntil is not null;

    public static AlertOutput Initial { get; } = new(AlertState.Normal, false, 0, false, null, false, null);
}
