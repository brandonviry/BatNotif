using BatNotif.Core.Alerts;

namespace BatNotif.Core.Settings;

/// <summary>Réglages utilisateur persistés (data-model §2, contracts/settings-schema.md).</summary>
public sealed record AppSettings
{
    public int LowThreshold { get; init; } = AlertConstants.DefaultLowThreshold;

    public int HighThreshold { get; init; } = AlertConstants.DefaultHighThreshold;

    /// <summary>Chemin du son de l'alerte basse ; vide = son intégré.</summary>
    public string LowSoundPath { get; init; } = "";

    /// <summary>Chemin du son de l'alerte haute ; vide = son intégré.</summary>
    public string HighSoundPath { get; init; } = "";

    /// <summary>Volume de 0.0 à 1.0.</summary>
    public double Volume { get; init; } = 0.8;

    public bool StartWithWindows { get; init; }

    public PauseState Pause { get; init; } = PauseState.None;

    public static AppSettings Default { get; } = new();

    /// <summary>Seuil de fin d'alerte basse (seuil bas + hystérésis).</summary>
    public int LowReset => LowThreshold + AlertConstants.Hysteresis;

    /// <summary>Seuil de fin d'alerte haute (seuil haut − hystérésis).</summary>
    public int HighReset => HighThreshold - AlertConstants.Hysteresis;
}
