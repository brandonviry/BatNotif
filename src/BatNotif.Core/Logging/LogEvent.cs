using System.Globalization;

namespace BatNotif.Core.Logging;

/// <summary>Types d'événements journalisés (data-model §5).</summary>
public enum LogEventKind
{
    AppStart,
    AppStop,
    StateChanged,
    AlertPresented,
    AlertPresentFailed,
    Snooze,
    PauseStarted,
    PauseEnded,
    SettingsSaved,
    SettingsReset,
    SoundFallback,
    HotkeyUnavailable,
    Info,
    Error,
}

/// <summary>Une entrée du journal local (FR-036).</summary>
public sealed record LogEvent(DateTimeOffset Timestamp, LogEventKind Kind, string Detail)
{
    /// <summary>Format d'une ligne : <c>&lt;Timestamp ISO 8601&gt; [&lt;Kind&gt;] &lt;Detail&gt;</c>.</summary>
    public string Format() =>
        $"{Timestamp.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture)} [{Kind}] {Detail}";

    public override string ToString() => Format();
}
