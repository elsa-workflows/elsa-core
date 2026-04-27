using Elsa.Common;
using Elsa.Workflows.Runtime.Services;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Quiescence;

public class IngressSourceRegistryTests
{
    private readonly ISystemClock _clock;

    public IngressSourceRegistryTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(DateTimeOffset.Parse("2026-04-24T10:00:00Z"));
    }

    [Fact(DisplayName = "Registration collects every IIngressSource from DI")]
    public void CollectsSourcesFromDi()
    {
        var a = CreateSource("a");
        var b = CreateSource("b");
        var sut = new IngressSourceRegistry(new Lazy<IEnumerable<IIngressSource>>(() => new[] { a, b }), _clock);

        Assert.Equal(2, sut.Sources.Count);
        Assert.Contains(a, sut.Sources);
        Assert.Contains(b, sut.Sources);
    }

    [Fact(DisplayName = "Duplicate source names throw on first materialization")]
    public void DuplicateNamesThrow()
    {
        // The registry is constructed with a Lazy<IEnumerable<IIngressSource>> to break a DI cycle (see
        // IngressSourceRegistry doc); materialization, and therefore the duplicate-name check, is deferred
        // until the first call that needs the underlying sources.
        var a = CreateSource("same");
        var b = CreateSource("same");
        var sut = new IngressSourceRegistry(new Lazy<IEnumerable<IIngressSource>>(() => new[] { a, b }), _clock);
        Assert.Throws<InvalidOperationException>(() => sut.Sources);
    }

    [Fact(DisplayName = "Snapshot reports per-source state")]
    public void SnapshotIncludesState()
    {
        var a = CreateSource("a");
        var sut = new IngressSourceRegistry(new Lazy<IEnumerable<IIngressSource>>(() => new[] { a }), _clock);

        sut.RecordTransition("a", IngressSourceState.Pausing);

        var snap = sut.Snapshot();
        Assert.Single(snap);
        Assert.Equal("a", snap.First().Name);
        Assert.Equal(IngressSourceState.Pausing, snap.First().State);
        Assert.Equal(_clock.UtcNow, snap.First().LastTransitionAt);
    }

    [Fact(DisplayName = "MarkPauseFailed captures error and transitions to PauseFailed")]
    public async Task MarkPauseFailedCapturesError()
    {
        var a = CreateSource("a");
        var sut = new IngressSourceRegistry(new Lazy<IEnumerable<IIngressSource>>(() => new[] { a }), _clock);
        var error = new TimeoutException("ingress timed out");

        await sut.MarkPauseFailedAsync("a", "timeout", error);

        var snap = sut.Snapshot();
        Assert.Equal(IngressSourceState.PauseFailed, snap.First().State);
        Assert.Same(error, snap.First().LastError);
    }

    [Fact(DisplayName = "MarkPauseFailed is concurrent-safe")]
    public async Task MarkPauseFailedConcurrent()
    {
        var a = CreateSource("a");
        var sut = new IngressSourceRegistry(new Lazy<IEnumerable<IIngressSource>>(() => new[] { a }), _clock);

        await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => sut.MarkPauseFailedAsync("a", "race").AsTask()));

        Assert.Equal(IngressSourceState.PauseFailed, sut.Snapshot().First().State);
    }

    [Fact(DisplayName = "Concurrent first access does not double-populate or throw duplicate")]
    public async Task ConcurrentFirstAccessIsSafe()
    {
        // Regression for the race in EnsureMaterialized: without a double-checked lock, two threads observing
        // _entries.Count == 0 would both iterate the source factory and the second TryAdd of every name would
        // throw "Duplicate ingress source registration".
        var a = CreateSource("a");
        var b = CreateSource("b");
        var c = CreateSource("c");
        var sut = new IngressSourceRegistry(new Lazy<IEnumerable<IIngressSource>>(() => new[] { a, b, c }), _clock);

        var startGate = new TaskCompletionSource();
        var readers = Enumerable.Range(0, 16).Select(_ => Task.Run(async () =>
        {
            await startGate.Task;
            return sut.Sources.Count;
        })).ToArray();

        startGate.SetResult();
        var counts = await Task.WhenAll(readers);

        Assert.All(counts, c => Assert.Equal(3, c));
    }

    private static IIngressSource CreateSource(string name)
    {
        var source = Substitute.For<IIngressSource>();
        source.Name.Returns(name);
        source.PauseTimeout.Returns(TimeSpan.FromSeconds(5));
        source.CurrentState.Returns(IngressSourceState.Running);
        return source;
    }
}
