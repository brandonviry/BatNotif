namespace BatNotif.Core.Logging;

/// <summary>
/// Destination du journal. Les implémentations ne doivent jamais lever d'exception :
/// un journal en panne ne doit pas empêcher les alertes.
/// </summary>
public interface IEventLog
{
    void Write(LogEvent logEvent);
}

public static class EventLogExtensions
{
    /// <summary>Écrit un événement horodaté par le <paramref name="time"/> fourni.</summary>
    public static void Write(this IEventLog log, TimeProvider time, LogEventKind kind, string detail) =>
        log.Write(new LogEvent(time.GetLocalNow(), kind, detail));
}
