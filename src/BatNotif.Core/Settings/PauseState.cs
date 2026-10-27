namespace BatNotif.Core.Settings;

/// <summary>Type de pause (data-model §4).</summary>
public enum PauseKind
{
    None,
    Timed,
    Indefinite,
}

/// <summary>Pause persistée dans les réglages pour survivre au redémarrage (FR-026).</summary>
/// <param name="Kind">Type de pause.</param>
/// <param name="Until">Fin de pause (UTC), requise si <see cref="PauseKind.Timed"/>.</param>
public sealed record PauseState(PauseKind Kind, DateTimeOffset? Until)
{
    public static PauseState None { get; } = new(PauseKind.None, null);

    public static PauseState Indefinite { get; } = new(PauseKind.Indefinite, null);

    public static PauseState For(TimeProvider time, TimeSpan duration) =>
        new(PauseKind.Timed, time.GetUtcNow() + duration);

    /// <summary>Vrai si la pause est en vigueur à l'instant <paramref name="now"/>.</summary>
    public bool IsActiveAt(DateTimeOffset now) => Kind switch
    {
        PauseKind.Indefinite => true,
        PauseKind.Timed => Until is { } until && until > now,
        _ => false,
    };
}
