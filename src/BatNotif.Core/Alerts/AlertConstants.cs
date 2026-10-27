namespace BatNotif.Core.Alerts;

/// <summary>Constantes du MVP, non réglables par l'utilisateur (data-model §2, research R12).</summary>
public static class AlertConstants
{
    /// <summary>Écart de fin d'alerte : fin à seuil bas + 2 % et seuil haut − 2 %.</summary>
    public const int Hysteresis = 2;

    public const int DefaultLowThreshold = 20;
    public const int DefaultHighThreshold = 80;

    public const int MinThreshold = 5;
    public const int MaxThreshold = 95;

    /// <summary>Écart minimal seuil haut − seuil bas pour que les zones de fin ne se chevauchent pas.</summary>
    public const int MinThresholdGap = 2 * Hysteresis + 1;

    public static readonly TimeSpan SnoozeDuration = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan AcDebounce = TimeSpan.FromSeconds(2);
    public static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
}
