namespace BatNotif.Core.Battery;

/// <summary>
/// Instantané de l'état batterie / secteur observé à un instant donné (data-model §1).
/// </summary>
/// <param name="Level">Niveau de charge en pourcentage, borné à 0–100.</param>
/// <param name="HasBattery">Faux sur une machine sans batterie (FR-005).</param>
/// <param name="AcPresent">Secteur branché.</param>
/// <param name="Charging">La batterie se charge réellement.</param>
/// <param name="Discharging">La batterie se vide réellement, même sur secteur (chargeur trop faible).</param>
/// <param name="ObservedAt">Moment de l'observation, fourni par le <see cref="TimeProvider"/>.</param>
public sealed record BatterySnapshot(
    int Level,
    bool HasBattery,
    bool AcPresent,
    bool Charging,
    bool Discharging,
    DateTimeOffset ObservedAt)
{
    public int Level { get; init; } = Math.Clamp(Level, 0, 100);

    /// <summary>
    /// Vrai si l'alerte basse doit raisonner « sur batterie » : pas de secteur,
    /// ou secteur présent mais batterie qui se décharge quand même (FR-006).
    /// </summary>
    public bool EffectivelyOnBattery => !AcPresent || Discharging;

    /// <summary>Compare le contenu en ignorant le moment de l'observation.</summary>
    public bool SameStateAs(BatterySnapshot? other) =>
        other is not null
        && Level == other.Level
        && HasBattery == other.HasBattery
        && AcPresent == other.AcPresent
        && Charging == other.Charging
        && Discharging == other.Discharging;
}
