using Elsa.Common;
using Elsa.Workflows.Runtime.Services;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Quiescence;

public class BurstRegistryTests
{
    private readonly ISystemClock _clock;
    private readonly IIngressSourceRegistry _sources;

    public BurstRegistryTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(DateTimeOffset.Parse("2026-04-24T10:00:00Z"));
        _sources = Substitute.For<IIngressSourceRegistry>();
        _sources.Snapshot().Returns(Array.Empty<IngressSourceSnapshot>());
    }

    [Fact(DisplayName = "Active count increases and decreases with begin/dispose")]
    public void ActiveCountFollowsBurstLifecycle()
    {
        var sut = new BurstRegistry(_sources, _clock);

        Assert.Equal(0, sut.ActiveCount);

        var a = sut.BeginBurst("instance-1", ingressSourceName: null, CancellationToken.None);
        Assert.Equal(1, sut.ActiveCount);

        var b = sut.BeginBurst("instance-2", ingressSourceName: null, CancellationToken.None);
        Assert.Equal(2, sut.ActiveCount);

        a.Dispose();
        Assert.Equal(1, sut.ActiveCount);

        b.Dispose();
        Assert.Equal(0, sut.ActiveCount);
    }

    [Fact(DisplayName = "Begin with null ingress name does NOT flip any source")]
    public void NullIngressNameDoesNotFlip()
    {
        var sut = new BurstRegistry(_sources, _clock);

        using var _ = sut.BeginBurst("instance-1", ingressSourceName: null, CancellationToken.None);

        _sources.DidNotReceive().MarkPauseFailedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Exception?>());
    }

    [Fact(DisplayName = "Begin from a Paused source flips it to PauseFailed (FR-018)")]
    public void PausedSourceDeliveringIsFlipped()
    {
        var now = _clock.UtcNow;
        var snapshot = new[] { new IngressSourceSnapshot("http.trigger", IngressSourceState.Paused, null, now) };
        _sources.Snapshot().Returns(snapshot);
        var sut = new BurstRegistry(_sources, _clock);

        using var _ = sut.BeginBurst("instance-1", ingressSourceName: "http.trigger", CancellationToken.None);

        _sources.Received(1).MarkPauseFailedAsync("http.trigger", "delivered-while-paused", Arg.Any<Exception?>());
    }

    [Fact(DisplayName = "Begin from a Running source does NOT flip")]
    public void RunningSourceIsNotFlipped()
    {
        var now = _clock.UtcNow;
        var snapshot = new[] { new IngressSourceSnapshot("http.trigger", IngressSourceState.Running, null, now) };
        _sources.Snapshot().Returns(snapshot);
        var sut = new BurstRegistry(_sources, _clock);

        using var _ = sut.BeginBurst("instance-1", ingressSourceName: "http.trigger", CancellationToken.None);

        _sources.DidNotReceive().MarkPauseFailedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Exception?>());
    }

    [Fact(DisplayName = "EnumerateActive returns a snapshot of live handles")]
    public void EnumerateActiveReturnsSnapshot()
    {
        var sut = new BurstRegistry(_sources, _clock);
        var a = sut.BeginBurst("instance-1", null, CancellationToken.None);
        var b = sut.BeginBurst("instance-2", null, CancellationToken.None);

        var snapshot = sut.EnumerateActive();

        Assert.Equal(2, snapshot.Count);
        Assert.Contains(a, snapshot);
        Assert.Contains(b, snapshot);

        a.Dispose();
        b.Dispose();
    }

    [Fact(DisplayName = "BurstHandle.Cancel triggers the cancellation token")]
    public void BurstHandleCancelFiresToken()
    {
        var sut = new BurstRegistry(_sources, _clock);
        using var handle = sut.BeginBurst("instance-1", null, CancellationToken.None);

        Assert.False(handle.CancellationToken.IsCancellationRequested);
        handle.Cancel();
        Assert.True(handle.CancellationToken.IsCancellationRequested);
    }
}
