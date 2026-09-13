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

    [Test]
    [DisplayName("Active count increases and decreases with begin/dispose")]
    public async Task ActiveCountFollowsExecutionCycleLifecycle()
    {
        var sut = new ExecutionCycleRegistry(_sources, _clock);

        await Assert.That(sut.ActiveCount).IsEqualTo(0);

        using var a = sut.BeginCycle("instance-1", ingressSourceName: null, CancellationToken.None);
        await Assert.That(sut.ActiveCount).IsEqualTo(1);

        using var b = sut.BeginCycle("instance-2", ingressSourceName: null, CancellationToken.None);
        await Assert.That(sut.ActiveCount).IsEqualTo(2);

        a.Dispose();
        await Assert.That(sut.ActiveCount).IsEqualTo(1);

        b.Dispose();
        await Assert.That(sut.ActiveCount).IsEqualTo(0);
    }

    [Test]
    [DisplayName("Begin with null ingress name does NOT flip any source")]
    public void NullIngressNameDoesNotFlip()
    {
        var sut = new ExecutionCycleRegistry(_sources, _clock);

        using var _ = sut.BeginCycle("instance-1", ingressSourceName: null, CancellationToken.None);

        _sources.DidNotReceive().MarkPauseFailedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Exception?>());
    }

    [Test]
    [DisplayName("Begin from a Paused source flips it to PauseFailed (FR-018)")]
    public void PausedSourceDeliveringIsFlipped()
    {
        var now = _clock.UtcNow;
        var snapshot = new[] { new IngressSourceSnapshot("http.trigger", IngressSourceState.Paused, null, now) };
        _sources.Snapshot().Returns(snapshot);
        var sut = new ExecutionCycleRegistry(_sources, _clock);

        using var _ = sut.BeginCycle("instance-1", ingressSourceName: "http.trigger", CancellationToken.None);

        _sources.Received(1).MarkPauseFailedAsync("http.trigger", "delivered-while-paused", Arg.Any<Exception?>());
    }

    [Test]
    [DisplayName("Begin from a Running source does NOT flip")]
    public void RunningSourceIsNotFlipped()
    {
        var now = _clock.UtcNow;
        var snapshot = new[] { new IngressSourceSnapshot("http.trigger", IngressSourceState.Running, null, now) };
        _sources.Snapshot().Returns(snapshot);
        var sut = new ExecutionCycleRegistry(_sources, _clock);

        using var _ = sut.BeginCycle("instance-1", ingressSourceName: "http.trigger", CancellationToken.None);

        _sources.DidNotReceive().MarkPauseFailedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Exception?>());
    }

    [Test]
    [DisplayName("ListActiveCycles returns a snapshot of live handles")]
    public async Task ListActiveCyclesReturnsSnapshot()
    {
        var sut = new ExecutionCycleRegistry(_sources, _clock);
        using var a = sut.BeginCycle("instance-1", null, CancellationToken.None);
        using var b = sut.BeginCycle("instance-2", null, CancellationToken.None);

        var snapshot = sut.ListActiveCycles();

        await Assert.That(snapshot.Count).IsEqualTo(2);
        await Assert.That(snapshot).Contains(a);
        await Assert.That(snapshot).Contains(b);

        a.Dispose();
        b.Dispose();
    }

    [Test]
    [DisplayName("ExecutionCycleHandle.Cancel triggers the cancellation token")]
    public async Task ExecutionCycleHandleCancelFiresToken()
    {
        var sut = new ExecutionCycleRegistry(_sources, _clock);
        using var handle = sut.BeginCycle("instance-1", null, CancellationToken.None);

        await Assert.That(handle.CancellationToken.IsCancellationRequested).IsFalse();
        handle.Cancel();
        await Assert.That(handle.CancellationToken.IsCancellationRequested).IsTrue();
    }

    [Test]
    [DisplayName("ExecutionCycleHandle.Cancel invokes the cancel callback supplied at registration")]
    public async Task CancelCallbackIsInvoked()
    {
        var sut = new ExecutionCycleRegistry(_sources, _clock);
        var callbackInvocations = 0;
        using var handle = sut.BeginCycle(
            "instance-1",
            ingressSourceName: null,
            linkedToken: CancellationToken.None,
            cancelCallback: () => Interlocked.Increment(ref callbackInvocations));

        handle.Cancel();
        await Assert.That(callbackInvocations).IsEqualTo(1);

        // Truly idempotent: a second Cancel() before Dispose() must NOT re-invoke the callback. The handle uses an
        // Interlocked _cancelled flag so callers can't accidentally trigger non-idempotent cancellation side effects.
        handle.Cancel();
        await Assert.That(callbackInvocations).IsEqualTo(1);

        // Once disposed, further Cancel() invocations remain silent no-ops.
        handle.Dispose();
        handle.Cancel();
        await Assert.That(callbackInvocations).IsEqualTo(1);
    }

    [Test]
    [DisplayName("ExecutionCycleHandle.Cancel swallows callback exceptions so drain is not interrupted")]
    public async Task CancelCallbackExceptionsAreSwallowed()
    {
        var sut = new ExecutionCycleRegistry(_sources, _clock);
        using var handle = sut.BeginCycle(
            "instance-1",
            ingressSourceName: null,
            linkedToken: CancellationToken.None,
            cancelCallback: () => throw new InvalidOperationException("activity refused to cancel"));

        // Should not throw — Cancel() must remain best-effort so a single misbehaving workflow does not crash drain.
        handle.Cancel();
        await Assert.That(handle.CancellationToken.IsCancellationRequested).IsTrue();
    }

    [Test]
    [DisplayName("ExecutionCycleHandle.Disposed completes when the handle is disposed")]
    public async Task DisposedTaskCompletesOnDispose()
    {
        var sut = new ExecutionCycleRegistry(_sources, _clock);
        using var handle = sut.BeginCycle("instance-1", null, CancellationToken.None);

        await Assert.That(handle.Disposed.IsCompleted).IsFalse();
        handle.Dispose();
        await handle.Disposed.WaitAsync(TimeSpan.FromSeconds(1));
        await Assert.That(handle.Disposed.IsCompletedSuccessfully).IsTrue();
    }
}
