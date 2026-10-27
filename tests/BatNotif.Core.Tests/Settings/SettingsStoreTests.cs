using System.Text.Json;
using BatNotif.Core.Logging;
using BatNotif.Core.Settings;
using BatNotif.Core.Tests.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace BatNotif.Core.Tests.Settings;

/// <summary>
/// FR-021, contracts/settings-schema.md. Les fichiers sont écrits dans un dossier temporaire
/// sous le dossier de sortie des tests (jamais en dehors du dépôt).
/// </summary>
public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(AppContext.BaseDirectory, "tmp-settings", Guid.NewGuid().ToString("N"));
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 27, 14, 30, 0, TimeSpan.Zero));
    private readonly InMemoryEventLog _log = new();

    private string SettingsPath => Path.Combine(_directory, "settings.json");

    private SettingsStore CreateStore() => new(SettingsPath, _log, _time);

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private void WriteRaw(string json)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(SettingsPath, json);
    }

    [Fact]
    public void Missing_file_gives_defaults_without_warning()
    {
        var result = CreateStore().Load();

        Assert.Equal(AppSettings.Default, result.Settings);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Save_then_load_round_trips_every_field()
    {
        var settings = new AppSettings
        {
            LowThreshold = 15,
            HighThreshold = 85,
            LowSoundPath = @"D:\musique\bas.mp3",
            HighSoundPath = @"D:\musique\haut.wav",
            Volume = 0.35,
            StartWithWindows = true,
            Pause = new PauseState(PauseKind.Timed, _time.GetUtcNow().AddHours(1)),
        };
        var store = CreateStore();

        store.Save(settings);
        var result = store.Load();

        Assert.Equal(settings, result.Settings);
        Assert.Empty(result.Warnings);
        Assert.Single(_log.OfKind(LogEventKind.SettingsSaved));
    }

    [Fact]
    public void Indefinite_pause_round_trips()
    {
        var store = CreateStore();

        store.Save(AppSettings.Default with { Pause = PauseState.Indefinite });

        Assert.Equal(PauseState.Indefinite, store.Load().Settings.Pause);
    }

    [Fact]
    public void Written_file_follows_the_contract_format()
    {
        CreateStore().Save(AppSettings.Default);

        using var json = JsonDocument.Parse(File.ReadAllText(SettingsPath));
        var root = json.RootElement;
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(20, root.GetProperty("lowThreshold").GetInt32());
        Assert.Equal(80, root.GetProperty("highThreshold").GetInt32());
        Assert.Equal("", root.GetProperty("lowSoundPath").GetString());
        Assert.Equal(0.8, root.GetProperty("volume").GetDouble());
        Assert.False(root.GetProperty("startWithWindows").GetBoolean());
        Assert.Equal("None", root.GetProperty("pause").GetProperty("kind").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("pause").GetProperty("until").ValueKind);
        Assert.Contains(Environment.NewLine + "  ", File.ReadAllText(SettingsPath)); // indenté, lisible
    }

    [Fact]
    public void Save_is_atomic_and_leaves_no_temporary_file()
    {
        var store = CreateStore();
        store.Save(AppSettings.Default);
        File.WriteAllText(SettingsPath + ".tmp", "reste d'un enregistrement interrompu");

        store.Save(AppSettings.Default with { LowThreshold = 10 });

        Assert.False(File.Exists(SettingsPath + ".tmp"));
        Assert.Equal(10, store.Load().Settings.LowThreshold);
    }

    [Fact]
    public void Unreadable_json_is_renamed_and_defaults_are_used()
    {
        WriteRaw("{ ceci n'est pas du JSON");

        var result = CreateStore().Load();

        Assert.Equal(AppSettings.Default, result.Settings);
        Assert.False(File.Exists(SettingsPath));
        Assert.True(File.Exists(Path.Combine(_directory, "settings.corrupt-20261027-143000.json")));
        Assert.Contains(result.Warnings, w => w.Contains("valeurs par défaut"));
        Assert.Single(_log.OfKind(LogEventKind.SettingsReset));
    }

    [Fact]
    public void Unknown_keys_are_ignored()
    {
        WriteRaw("""{ "schemaVersion": 1, "lowThreshold": 25, "highThreshold": 85, "volume": 0.5, "startWithWindows": false, "couleur": "bleu" }""");

        var result = CreateStore().Load();

        Assert.Equal(25, result.Settings.LowThreshold);
        Assert.Equal(85, result.Settings.HighThreshold);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Missing_keys_take_their_default()
    {
        WriteRaw("""{ "schemaVersion": 1, "highThreshold": 90 }""");

        var result = CreateStore().Load();

        Assert.Equal(20, result.Settings.LowThreshold);
        Assert.Equal(90, result.Settings.HighThreshold);
        Assert.Equal(0.8, result.Settings.Volume);
        Assert.Equal(PauseState.None, result.Settings.Pause);
    }

    [Fact]
    public void Out_of_range_field_takes_its_default_with_warning()
    {
        WriteRaw("""{ "schemaVersion": 1, "lowThreshold": 20, "highThreshold": 80, "volume": 7 }""");

        var result = CreateStore().Load();

        Assert.Equal(0.8, result.Settings.Volume);
        Assert.Single(result.Warnings);
        Assert.Contains("volume", result.Warnings[0]);
    }

    [Fact]
    public void Wrong_type_field_takes_its_default_with_warning()
    {
        WriteRaw("""{ "schemaVersion": 1, "lowThreshold": "vingt", "highThreshold": 80 }""");

        var result = CreateStore().Load();

        Assert.Equal(20, result.Settings.LowThreshold);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void Incoherent_thresholds_reset_both_with_warning()
    {
        WriteRaw("""{ "schemaVersion": 1, "lowThreshold": 79, "highThreshold": 80 }""");

        var result = CreateStore().Load();

        Assert.Equal(20, result.Settings.LowThreshold);
        Assert.Equal(80, result.Settings.HighThreshold);
        Assert.Contains(result.Warnings, w => w.Contains("seuil"));
    }

    [Fact]
    public void Expired_timed_pause_is_treated_as_none()
    {
        WriteRaw("""{ "schemaVersion": 1, "pause": { "kind": "Timed", "until": "2026-10-27T14:00:00Z" } }""");

        var result = CreateStore().Load();

        Assert.Equal(PauseState.None, result.Settings.Pause);
    }

    [Fact]
    public void Active_timed_pause_is_kept()
    {
        WriteRaw("""{ "schemaVersion": 1, "pause": { "kind": "Timed", "until": "2026-10-27T15:00:00Z" } }""");

        var result = CreateStore().Load();

        Assert.Equal(PauseKind.Timed, result.Settings.Pause.Kind);
        Assert.Equal(new DateTimeOffset(2026, 10, 27, 15, 0, 0, TimeSpan.Zero), result.Settings.Pause.Until);
    }
}
