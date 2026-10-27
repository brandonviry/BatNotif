using BatNotif.Core.Logging;
using Microsoft.Win32;

namespace BatNotif.App.Platform;

/// <summary>
/// Démarrage avec Windows via <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c> (FR-022, research R8) :
/// aucun droit administrateur. Les erreurs sont journalisées, jamais levées.
/// </summary>
public sealed class AutostartRegistry(IEventLog log, TimeProvider time)
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "BatNotif";

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string;
        }
        catch (Exception ex)
        {
            log.Write(time, LogEventKind.Error, $"Lecture du démarrage automatique impossible — {ex.Message}");
            return false;
        }
    }

    /// <summary>Active ou désactive le lancement à l'ouverture de session ; renvoie vrai si l'opération a réussi.</summary>
    public bool Apply(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
            if (enabled)
            {
                var exe = Environment.ProcessPath ?? throw new InvalidOperationException("Chemin de l'exécutable inconnu.");
                key.SetValue(ValueName, $"\"{exe}\"");
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            return true;
        }
        catch (Exception ex)
        {
            log.Write(time, LogEventKind.Error, $"Modification du démarrage automatique impossible — {ex.Message}");
            return false;
        }
    }
}
