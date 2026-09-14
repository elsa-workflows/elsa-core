using Elsa.Common;
using Elsa.Workflows.Runtime.Services;
using NSubstitute;
using System.Threading.Tasks;

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

        var a = sut.BeginCycle("instance-1", ingressSourceName: null, CancellationToken.None);
        await Assert.That(sut.ActiveCount).IsEqualTo(1);

        var b = sut.BeginCycle("instance-2", ingressSourceName: null, CancellationToken.None);
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
        var a = sut.BeginCycle("instance-1", null, CancellationToken.None);
        var b = sut.BeginCycle("instance-2", null, CancellationToken.None);

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
    [DisplayName("ExecutionCycleHandle.TryCancel reports whether this call transitioned the handle")]
    public async Task ExecutionCycleHandleTryCancelReportsTransition()
    {
        var sut = new ExecutionCycleRegistry(_sources, _clock);
        var handle = sut.BeginCycle("instance-1", null, CancellationToken.None);

        await Assert.That(handle.TryCancel()).IsTrue();
        await Assert.That(handle.TryCancel()).IsFalse();

        handle.Dispose();
        await Assert.That(handle.TryCancel()).IsFalse();

        var disposed = sut.BeginCycle("instance-2", null, CancellationToken.None);
        disposed.Dispose();
        await Assert.That(disposed.TryCancel()).IsFalse();
    }

    [Test]
    [DisplayName("ExecutionCycleHandle.TryCancel reports false when disposed during the cancellation callback")]
    public async Task TryCancelReportsFalseWhenDisposedDuringCancellationCallback()
    {
        var sut = new ExecutionCycleRegistry(_sources, _clock);
        var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handle = sut.BeginCycle(
            "instance-1",
            ingressSourceName: null,
            linkedToken: CancellationToken.None,
            cancelCallback: () =>
            {
                callbackEntered.SetResult();
                releaseCallback.Task.GetAwaiter().GetResult();
            });

        var cancelTask = Task.Run(handle.TryCancel);
        await callbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        handle.Dispose();
        releaseCallback.SetResult();

        await Assert.That(await cancelTask.WaitAsync(TimeSpan.FromSeconds(5))).IsFalse();
    }

    [Test]
    [DisplayName("ExecutionCycleHandle.Dispose completes while a CTS callback waits for it")]
    public async Task DisposeCompletesWhileCtsCallbackWaitsForIt()
    {
        var cancellationCallbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var disposalCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackObservedDisposal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handle = new ExecutionCycleHandle(
            Guid.NewGuid(),
            "instance-1",
            ingressSourceName: null,
            startedAt: DateTimeOffset.UtcNow,
            linkedToken: CancellationToken.None,
            onDisposed: null);
        using var registration = handle.CancellationToken.Register(() =>
        {
            cancellationCallbackEntered.SetResult();
            callbackObservedDisposal.SetResult(disposalCompleted.Task.Wait(TimeSpan.FromSeconds(5)));
        });

        var cancelTask = Task.Run(handle.TryCancel);
        await cancellationCallbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var disposeTask = Task.Run(() =>
        {
            handle.Dispose();
            disposalCompleted.TrySetResult();
        });

        await Assert.That(await callbackObservedDisposal.Task.WaitAsync(TimeSpan.FromSeconds(5))).IsTrue();
        await disposeTask.WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.That(await cancelTask.WaitAsync(TimeSpan.FromSeconds(5))).IsFalse();
        await Assert.That(handle.Disposed.IsCompletedSuccessfully).IsTrue();
    }

    [Test]
    [DisplayName("ExecutionCycleHandle defers CTS disposal while linked-token cancellation is in progress")]
    public async Task DisposeCompletesWhileLinkedTokenCancellationWaitsForIt()
    {
        using var linkedCts = new CancellationTokenSource();
        var cancellationCallbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var disposalCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackObservedDisposal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handle = new ExecutionCycleHandle(
            Guid.NewGuid(),
            "instance-linked-cancellation",
            ingressSourceName: null,
            startedAt: DateTimeOffset.UtcNow,
            linkedToken: linkedCts.Token);
        var cycleToken = handle.CancellationToken;
        using var registration = cycleToken.Register(() =>
        {
            cancellationCallbackEntered.SetResult();
            callbackObservedDisposal.SetResult(disposalCompleted.Task.Wait(TimeSpan.FromSeconds(5)));
        });

        var cancelTask = Task.Run(() => linkedCts.Cancel());
        await cancellationCallbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var disposeTask = Task.Run(() =>
        {
            handle.Dispose();
            disposalCompleted.TrySetResult();
        });

        await Assert.That(await callbackObservedDisposal.Task.WaitAsync(TimeSpan.FromSeconds(5))).IsTrue();
        await disposeTask.WaitAsync(TimeSpan.FromSeconds(5));
        await cancelTask.WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.That(handle.Disposed.IsCompletedSuccessfully).IsTrue();
        await Assert.That(cycleToken.IsCancellationRequested).IsTrue();
    }

    [Test]
    [DisplayName("ExecutionCycleHandle defers CTS disposal from a cancellation callback")]
    public async Task DisposeDefersCtsDisposalUntilCancellationPropagationExits()
    {
        var disposedDuringCancellation = false;
        var handle = new ExecutionCycleHandle(
            Guid.NewGuid(),
            "instance-1",
            ingressSourceName: null,
            startedAt: DateTimeOffset.UtcNow,
            linkedToken: CancellationToken.None);

        using var registration = handle.CancellationToken.Register(() =>
        {
            handle.Dispose();
            disposedDuringCancellation = handle.Disposed.IsCompleted;
        });

        await Assert.That(handle.TryCancel()).IsFalse();
        await Assert.That(disposedDuringCancellation).IsFalse();
        await Assert.That(handle.Disposed.IsCompletedSuccessfully).IsTrue();
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
        // Interlocked lifecycle state so callers can't accidentally trigger non-idempotent cancellation side effects.
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
    [DisplayName("ExecutionCycleHandle.TryCancel swallows non-fatal CTS callback exceptions")]
    public async Task TryCancelSwallowsNonFatalCtsCallbackExceptions()
    {
        var handle = new ExecutionCycleHandle(
            Guid.NewGuid(),
            "instance-1",
            ingressSourceName: null,
            startedAt: DateTimeOffset.UtcNow,
            linkedToken: CancellationToken.None);
        using var registration = handle.CancellationToken.Register(() => throw new InvalidOperationException("callback refused to cancel"));

        await Assert.That(handle.TryCancel()).IsTrue();
        await Assert.That(handle.TryCancel()).IsFalse();

        handle.Dispose();
        await Assert.That(handle.Disposed.IsCompletedSuccessfully).IsTrue();
    }

    [Test]
    [DisplayName("ExecutionCycleHandle.TryCancel propagates fatal CTS callback exceptions wrapped in an aggregate")]
    public async Task TryCancelPropagatesFatalCtsCallbackExceptions()
    {
        var handle = new ExecutionCycleHandle(
            Guid.NewGuid(),
            "instance-1",
            ingressSourceName: null,
            startedAt: DateTimeOffset.UtcNow,
            linkedToken: CancellationToken.None);
        using var registration = handle.CancellationToken.Register(() => throw new OutOfMemoryException("fatal callback failure"));

        var exception = await Assert.That(() => handle.TryCancel()).ThrowsExactly<AggregateException>();

        await Assert.That(exception.Flatten().InnerExceptions).Contains(inner => inner is OutOfMemoryException);
        handle.Dispose();
        await Assert.That(handle.Disposed.IsCompletedSuccessfully).IsTrue();
    }

    [Test]
    [DisplayName("ExecutionCycleHandle.Disposed completes when the handle is disposed")]
    public async Task DisposedTaskCompletesOnDispose()
    {
        var sut = new ExecutionCycleRegistry(_sources, _clock);
        var handle = sut.BeginCycle("instance-1", null, CancellationToken.None);

        await Assert.That(handle.Disposed.IsCompleted).IsFalse();
        handle.Dispose();
        await handle.Disposed.WaitAsync(TimeSpan.FromSeconds(1));
        await Assert.That(handle.Disposed.IsCompletedSuccessfully).IsTrue();
    }
}
