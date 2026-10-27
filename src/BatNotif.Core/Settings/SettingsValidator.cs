using BatNotif.Core.Alerts;

namespace BatNotif.Core.Settings;

/// <summary>Résultat de validation ; messages en français destinés à l'utilisateur.</summary>
public sealed record ValidationResult(IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

/// <summary>Validation des réglages saisis (FR-018, research R12).</summary>
public static class SettingsValidator
{
    public const string GapMessage = "Le seuil haut doit dépasser le seuil bas d'au moins 5 %.";

    public static ValidationResult Validate(AppSettings settings)
    {
        var errors = new List<string>();
        var lowOk = IsThresholdInRange(settings.LowThreshold);
        var highOk = IsThresholdInRange(settings.HighThreshold);

        if (!lowOk)
        {
            errors.Add(RangeMessage("bas"));
        }

        if (!highOk)
        {
            errors.Add(RangeMessage("haut"));
        }

        if (lowOk && highOk && settings.HighThreshold - settings.LowThreshold < AlertConstants.MinThresholdGap)
        {
            errors.Add(GapMessage);
        }

        if (!IsVolumeInRange(settings.Volume))
        {
            errors.Add("Le volume doit être compris entre 0 et 100 %.");
        }

        return new ValidationResult(errors);
    }

    public static bool IsThresholdInRange(int value) =>
        value is >= AlertConstants.MinThreshold and <= AlertConstants.MaxThreshold;

    public static bool IsVolumeInRange(double volume) => volume is >= 0.0 and <= 1.0;

    private static string RangeMessage(string which) =>
        $"Le seuil {which} doit être compris entre {AlertConstants.MinThreshold} % et {AlertConstants.MaxThreshold} %.";
}
