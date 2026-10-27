using BatNotif.Core.Logging;

namespace BatNotif.Core.Tests.Fakes;

/// <summary>Journal en mémoire pour vérifier les événements écrits.</summary>
public sealed class InMemoryEventLog : IEventLog
{
    private readonly List<LogEvent> _events = [];

    public IReadOnlyList<LogEvent> Events => _events;

    public void Write(LogEvent logEvent) => _events.Add(logEvent);

    public IEnumerable<LogEvent> OfKind(LogEventKind kind) => _events.Where(e => e.Kind == kind);
}
