using System.Globalization;
using System.Text;
using System.Text.Json;
using BatNotif.Core.Alerts;
using BatNotif.Core.Logging;

namespace BatNotif.Core.Settings;

/// <summary>Résultat du chargement : réglages utilisables + avertissements pour l'utilisateur.</summary>
public sealed record SettingsLoadResult(AppSettings Settings, IReadOnlyList<string> Warnings);

/// <summary>
/// Lecture / écriture de settings.json selon contracts/settings-schema.md (FR-021, research R10).
/// Lecture tolérante champ par champ ; écriture atomique (fichier .tmp puis remplacement).
/// </summary>
public sealed class SettingsStore
{
    public const int SchemaVersion = 1;

    private readonly string _path;
    private readonly IEventLog _log;
    private readonly TimeProvider _time;

    public SettingsStore(string path, IEventLog log, TimeProvider time)
    {
        _path = path;
        _log = log;
        _time = time;
    }

    public string Path => _path;

    public SettingsLoadResult Load()
    {
        if (!File.Exists(_path))
        {
            return new SettingsLoadResult(AppSettings.Default, []);
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(_path, Encoding.UTF8));
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or DecoderFallbackException)
        {
            return ResetCorruptFile(ex);
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return ResetCorruptFile(new JsonException("La racine n'est pas un objet JSON."));
            }

            return Read(document.RootElement);
        }
    }

    public void Save(AppSettings settings)
    {
        var directory = System.IO.Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporary = _path + ".tmp";
        using (var stream = File.Create(temporary))
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            Write(writer, settings);
        }

        File.Move(temporary, _path, overwrite: true);
        _log.Write(_time, LogEventKind.SettingsSaved,
            $"Réglages enregistrés : seuils {settings.LowThreshold} % / {settings.HighThreshold} %, volume {settings.Volume:P0}, " +
            $"démarrage Windows {(settings.StartWithWindows ? "oui" : "non")}, pause {settings.Pause.Kind}");
    }

    private SettingsLoadResult ResetCorruptFile(Exception cause)
    {
        var backupName = $"settings.corrupt-{_time.GetLocalNow():yyyyMMdd-HHmmss}.json";
        var backupPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_path) ?? "", backupName);
        try
        {
            File.Move(_path, backupPath, overwrite: true);
        }
        catch (Exception moveError) when (moveError is IOException or UnauthorizedAccessException)
        {
            backupName = "(impossible de le renommer)";
        }

        _log.Write(_time, LogEventKind.SettingsReset, $"Réglages illisibles, valeurs par défaut rétablies ; ancien fichier : {backupName} — {cause.Message}");
        return new SettingsLoadResult(
            AppSettings.Default,
            [$"Réglages illisibles : valeurs par défaut rétablies (ancien fichier conservé : {backupName})."]);
    }

    private SettingsLoadResult Read(JsonElement root)
    {
        var warnings = new List<string>();
        var defaults = AppSettings.Default;

        var low = ReadInt(root, "lowThreshold", defaults.LowThreshold, SettingsValidator.IsThresholdInRange, "seuil bas", warnings);
        var high = ReadInt(root, "highThreshold", defaults.HighThreshold, SettingsValidator.IsThresholdInRange, "seuil haut", warnings);
        if (high - low < AlertConstants.MinThresholdGap)
        {
            warnings.Add($"Seuils enregistrés incohérents ({low} % / {high} %) : seuils par défaut rétablis.");
            low = defaults.LowThreshold;
            high = defaults.HighThreshold;
        }

        var volume = ReadDouble(root, "volume", defaults.Volume, SettingsValidator.IsVolumeInRange, "volume", warnings);

        var settings = new AppSettings
        {
            LowThreshold = low,
            HighThreshold = high,
            LowSoundPath = ReadString(root, "lowSoundPath"),
            HighSoundPath = ReadString(root, "highSoundPath"),
            Volume = volume,
            StartWithWindows = ReadBool(root, "startWithWindows", defaults.StartWithWindows, warnings),
            Pause = ReadPause(root, warnings),
        };

        return new SettingsLoadResult(settings, warnings);
    }

    private static int ReadInt(JsonElement root, string name, int fallback, Func<int, bool> isValid, string label, List<string> warnings)
    {
        if (!root.TryGetProperty(name, out var element))
        {
            return fallback;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value) && isValid(value))
        {
            return value;
        }

        warnings.Add($"Valeur enregistrée invalide pour le {label} : valeur par défaut ({fallback}) rétablie.");
        return fallback;
    }

    private static double ReadDouble(JsonElement root, string name, double fallback, Func<double, bool> isValid, string label, List<string> warnings)
    {
        if (!root.TryGetProperty(name, out var element))
        {
            return fallback;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var value) && isValid(value))
        {
            return value;
        }

        warnings.Add($"Valeur enregistrée invalide pour le {label} : valeur par défaut ({fallback:P0}) rétablie.");
        return fallback;
    }

    private static bool ReadBool(JsonElement root, string name, bool fallback, List<string> warnings)
    {
        if (!root.TryGetProperty(name, out var element))
        {
            return fallback;
        }

        if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return element.GetBoolean();
        }

        warnings.Add("Valeur enregistrée invalide pour « Démarrer avec Windows » : option désactivée.");
        return fallback;
    }

    private static string ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? ""
            : "";

    private PauseState ReadPause(JsonElement root, List<string> warnings)
    {
        if (!root.TryGetProperty("pause", out var pause) || pause.ValueKind != JsonValueKind.Object)
        {
            return PauseState.None;
        }

        var kindText = pause.TryGetProperty("kind", out var kind) && kind.ValueKind == JsonValueKind.String ? kind.GetString() : null;
        switch (kindText)
        {
            case nameof(PauseKind.Indefinite):
                return PauseState.Indefinite;
            case nameof(PauseKind.Timed):
                if (pause.TryGetProperty("until", out var until)
                    && until.ValueKind == JsonValueKind.String
                    && DateTimeOffset.TryParse(until.GetString(), CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var end))
                {
                    var state = new PauseState(PauseKind.Timed, end);
                    return state.IsActiveAt(_time.GetUtcNow()) ? state : PauseState.None; // Pause expirée ignorée.
                }

                warnings.Add("Pause enregistrée invalide : ignorée.");
                return PauseState.None;
            case null or nameof(PauseKind.None):
                return PauseState.None;
            default:
                warnings.Add("Pause enregistrée invalide : ignorée.");
                return PauseState.None;
        }
    }

    private static void Write(Utf8JsonWriter writer, AppSettings settings)
    {
        writer.WriteStartObject();
        writer.WriteNumber("schemaVersion", SchemaVersion);
        writer.WriteNumber("lowThreshold", settings.LowThreshold);
        writer.WriteNumber("highThreshold", settings.HighThreshold);
        writer.WriteString("lowSoundPath", settings.LowSoundPath);
        writer.WriteString("highSoundPath", settings.HighSoundPath);
        writer.WriteNumber("volume", settings.Volume);
        writer.WriteBoolean("startWithWindows", settings.StartWithWindows);
        writer.WriteStartObject("pause");
        writer.WriteString("kind", settings.Pause.Kind.ToString());
        if (settings.Pause is { Kind: PauseKind.Timed, Until: { } until })
        {
            writer.WriteString("until", until.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNull("until");
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}
