using Elsa.Common;
using Elsa.Workflows.Runtime.Services;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Quiescence;

public class ExecutionCycleRegistryTests
{
    private readonly ISystemClock _clock;
    private readonly IIngressSourceRegistry _sources;

    public ExecutionCycleRegistryTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(DateTimeOffset.Parse("2026-04-24T10:00:00Z"));
        _sources = Substitute.For<IIngressSourceRegistry>();
        _sources.Snapshot().Returns(Array.Empty<IngressSourceSnapshot>());
    }

    [Fact(DisplayName = "Active count increases and decreases with begin/dispose")]
    public void ActiveCountFollowsExecutionCycleLifecycle()
    {
        var sut = new ExecutionCycleRegistry(_sources, _clock);

        Assert.Equal(0, sut.ActiveCount);

        var a = sut.BeginCycle("instance-1", ingressSourceName: null, CancellationToken.None);
        Assert.Equal(1, sut.ActiveCount);

        var b = sut.BeginCycle("instance-2", ingressSourceName: null, CancellationToken.None);
        Assert.Equal(2, sut.ActiveCount);

        a.Dispose();
        Assert.Equal(1, sut.ActiveCount);

        b.Dispose();
        Assert.Equal(0, sut.ActiveCount);
    }

    [Fact(DisplayName = "Begin with null ingress name does NOT flip any source")]
    public void NullIngressNameDoesNotFlip()
    {
        var sut = new ExecutionCycleRegistry(_sources, _clock);

        using var _ = sut.BeginCycle("instance-1", ingressSourceName: null, CancellationToken.None);

        _sources.DidNotReceive().MarkPauseFailedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Exception?>());
    }

    [Fact(DisplayName = "Begin from a Paused source flips it to PauseFailed (FR-018)")]
    public void PausedSourceDeliveringIsFlipped()
    {
        var now = _clock.UtcNow;
        var snapshot = new[] { new IngressSourceSnapshot("http.trigger", IngressSourceState.Paused, null, now) };
        _sources.Snapshot().Returns(snapshot);
        var sut = new ExecutionCycleRegistry(_sources, _clock);

        using var _ = sut.BeginCycle("instance-1", ingressSourceName: "http.trigger", CancellationToken.None);

        _sources.Received(1).MarkPauseFailedAsync("http.trigger", "delivered-while-paused", Arg.Any<Exception?>());
    }

    [Fact(DisplayName = "Begin from a Running source does NOT flip")]
    public void RunningSourceIsNotFlipped()
    {
        var now = _clock.UtcNow;
        var snapshot = new[] { new IngressSourceSnapshot("http.trigger", IngressSourceState.Running, null, now) };
        _sources.Snapshot().Returns(snapshot);
        var sut = new ExecutionCycleRegistry(_sources, _clock);

        using var _ = sut.BeginCycle("instance-1", ingressSourceName: "http.trigger", CancellationToken.None);

        _sources.DidNotReceive().MarkPauseFailedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Exception?>());
    }

    [Fact(DisplayName = "ListActiveCycles returns a snapshot of live handles")]
    public void ListActiveCyclesReturnsSnapshot()
    {
        var sut = new ExecutionCycleRegistry(_sources, _clock);
        var a = sut.BeginCycle("instance-1", null, CancellationToken.None);
        var b = sut.BeginCycle("instance-2", null, CancellationToken.None);

        var snapshot = sut.ListActiveCycles();

        Assert.Equal(2, snapshot.Count);
        Assert.Contains(a, snapshot);
        Assert.Contains(b, snapshot);

        a.Dispose();
        b.Dispose();
    }

    [Fact(DisplayName = "ExecutionCycleHandle.Cancel triggers the cancellation token")]
    public void ExecutionCycleHandleCancelFiresToken()
    {
        var sut = new ExecutionCycleRegistry(_sources, _clock);
        using var handle = sut.BeginCycle("instance-1", null, CancellationToken.None);

        Assert.False(handle.CancellationToken.IsCancellationRequested);
        handle.Cancel();
        Assert.True(handle.CancellationToken.IsCancellationRequested);
    }

    [Fact(DisplayName = "ExecutionCycleHandle.Cancel invokes the cancel callback supplied at registration")]
    public void CancelCallbackIsInvoked()
    {
        var sut = new ExecutionCycleRegistry(_sources, _clock);
        var callbackInvocations = 0;
        using var handle = sut.BeginCycle(
            "instance-1",
            ingressSourceName: null,
            linkedToken: CancellationToken.None,
            cancelCallback: () => Interlocked.Increment(ref callbackInvocations));

        handle.Cancel();
        Assert.Equal(1, callbackInvocations);

        // Truly idempotent: a second Cancel() before Dispose() must NOT re-invoke the callback. The handle uses an
        // Interlocked _cancelled flag so callers can't accidentally trigger non-idempotent cancellation side effects.
        handle.Cancel();
        Assert.Equal(1, callbackInvocations);

        // Once disposed, further Cancel() invocations remain silent no-ops.
        handle.Dispose();
        handle.Cancel();
        Assert.Equal(1, callbackInvocations);
    }

    [Fact(DisplayName = "ExecutionCycleHandle.Cancel swallows callback exceptions so drain is not interrupted")]
    public void CancelCallbackExceptionsAreSwallowed()
    {
        var sut = new ExecutionCycleRegistry(_sources, _clock);
        using var handle = sut.BeginCycle(
            "instance-1",
            ingressSourceName: null,
            linkedToken: CancellationToken.None,
            cancelCallback: () => throw new InvalidOperationException("activity refused to cancel"));

        // Should not throw — Cancel() must remain best-effort so a single misbehaving workflow does not crash drain.
        handle.Cancel();
        Assert.True(handle.CancellationToken.IsCancellationRequested);
    }

    [Fact(DisplayName = "ExecutionCycleHandle.Disposed completes when the handle is disposed")]
    public async Task DisposedTaskCompletesOnDispose()
    {
        var sut = new ExecutionCycleRegistry(_sources, _clock);
        var handle = sut.BeginCycle("instance-1", null, CancellationToken.None);

        Assert.False(handle.Disposed.IsCompleted);
        handle.Dispose();
        await handle.Disposed.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.True(handle.Disposed.IsCompletedSuccessfully);
    }
}
