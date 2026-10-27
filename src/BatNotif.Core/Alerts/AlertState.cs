namespace BatNotif.Core.Alerts;

/// <summary>États de la machine à états du moteur (data-model §3).</summary>
public enum AlertState
{
    /// <summary>Aucune condition d'alerte.</summary>
    Normal,

    /// <summary>Sur batterie (effectivement) et niveau ≤ seuil bas.</summary>
    AlertLow,

    /// <summary>Secteur présent et niveau ≥ seuil haut.</summary>
    AlertHigh,

    /// <summary>Aucune batterie détectée.</summary>
    NoBattery,
}
