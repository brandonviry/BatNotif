using System.Globalization;
using System.IO;
using System.Text;
using BatNotif.Core.Logging;

namespace BatNotif.App.Logging;

/// <summary>
/// Journal fichier : <c>%AppData%\BatNotif\logs\batnotif-&lt;yyyy-MM-dd&gt;.log</c>, une ligne par événement,
/// conservation 14 jours (research R10, FR-036). Ne lève jamais d'exception.
/// </summary>
public sealed class FileEventLog : IEventLog
{
    private static readonly TimeSpan Retention = TimeSpan.FromDays(14);

    private readonly string _directory;
    private readonly Lock _gate = new();

    public FileEventLog(string? directory = null)
    {
        _directory = directory ?? DefaultDirectory;
        try
        {
            Directory.CreateDirectory(_directory);
            PurgeOldFiles();
        }
        catch
        {
            // Un journal indisponible ne doit jamais empêcher l'application de tourner.
        }
    }

    public static string DefaultDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BatNotif", "logs");

    public string LogDirectory => _directory;

    public void Write(LogEvent logEvent)
    {
        try
        {
            var file = Path.Combine(
                _directory,
                $"batnotif-{logEvent.Timestamp.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.log");
            lock (_gate)
            {
                File.AppendAllText(file, logEvent.Format() + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
            // Voir constructeur.
        }
    }

    private void PurgeOldFiles()
    {
        var limit = DateTime.Now - Retention;
        foreach (var file in Directory.EnumerateFiles(_directory, "batnotif-*.log"))
        {
            if (File.GetLastWriteTime(file) < limit)
            {
                File.Delete(file);
            }
        }
    }
}
