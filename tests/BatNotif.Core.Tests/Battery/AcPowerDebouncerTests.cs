using BatNotif.Core.Battery;
using Microsoft.Extensions.Time.Testing;

namespace BatNotif.Core.Tests.Battery;

/// <summary>FR-003 : un changement de secteur de moins de 2 s est ignoré.</summary>
public class AcPowerDebouncerTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero));
    private readonly List<bool> _changes = [];

    private AcPowerDebouncer Create(bool initial) => new(_time, initial, _changes.Add);

    [Fact]
    public void Initial_value_is_stable_immediately()
    {
        using var debouncer = Create(initial: true);

        Assert.True(debouncer.Stable);
        Assert.Empty(_changes);
    }

    [Fact]
    public void Change_shorter_than_2s_is_ignored()
    {
        using var debouncer = Create(initial: false);

        debouncer.Observe(true);
        _time.Advance(TimeSpan.FromMilliseconds(1999));

        Assert.False(debouncer.Stable);
        Assert.Empty(_changes);
    }

    [Fact]
    public void Change_lasting_2s_becomes_stable_and_notifies_once()
    {
        using var debouncer = Create(initial: false);

        debouncer.Observe(true);
        _time.Advance(TimeSpan.FromSeconds(2));

        Assert.True(debouncer.Stable);
        Assert.Equal([true], _changes);

        debouncer.Observe(true);
        _time.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal([true], _changes);
    }

    [Fact]
    public void Micro_cuts_are_ignored()
    {
        using var debouncer = Create(initial: true);

        for (var i = 0; i < 5; i++)
        {
            debouncer.Observe(false);
            _time.Advance(TimeSpan.FromMilliseconds(800));
            debouncer.Observe(true);
            _time.Advance(TimeSpan.FromMilliseconds(800));
        }

        _time.Advance(TimeSpan.FromSeconds(10));
        Assert.True(debouncer.Stable);
        Assert.Empty(_changes);
    }

    [Fact]
    public void Repeated_observation_of_pending_value_does_not_restart_the_delay()
    {
        using var debouncer = Create(initial: false);

        debouncer.Observe(true);
        _time.Advance(TimeSpan.FromSeconds(1));
        debouncer.Observe(true);
        _time.Advance(TimeSpan.FromSeconds(1));

        Assert.True(debouncer.Stable);
    }

    [Fact]
    public void Reset_applies_a_value_immediately_without_notification()
    {
        using var debouncer = Create(initial: false);
        debouncer.Observe(true);

        debouncer.Reset(true);
        _time.Advance(TimeSpan.FromSeconds(5));

        Assert.True(debouncer.Stable);
        Assert.Empty(_changes);
    }
}
