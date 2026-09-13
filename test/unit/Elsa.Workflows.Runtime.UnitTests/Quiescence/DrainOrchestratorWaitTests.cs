using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.HostedServices;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Quiescence;

public class DrainOrchestratorWaitTests : DrainOrchestratorTestsBase
{
    [Fact(DisplayName = "Wait returns CompletedWithinDeadline when execution cycle count reaches zero before deadline")]
    public async Task ExecutionCyclesCompleteBeforeDeadline()
    {
        // Simulate two active execution cycles that drain to zero on the second poll iteration.
        var calls = 0;
        ExecutionCycleRegistry.ActiveCount.Returns(_ => calls++ switch { 0 => 2, 1 => 0, _ => 0 });

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.HostStopSignal);

        Assert.Equal(DrainResult.CompletedWithinDeadline, outcome.OverallResult);
        Assert.Equal(0, outcome.ExecutionCyclesForceCancelledCount);
    }

    [Fact(DisplayName = "Operator force trigger always reports Forced result and force-cancels live execution cycles")]
    public async Task ForceTriggerCancelsAllCycles()
    {
        var handle = new ExecutionCycleHandle(Guid.NewGuid(), "instance-1", ingressSourceName: "http.trigger", startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None);
        ExecutionCycleRegistry.ActiveCount.Returns(1);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle });
        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(new WorkflowInstance
            {
                Id = "instance-1",
                DefinitionId = "def-1",
                DefinitionVersionId = "ver-1",
                Version = 1,
                IsExecuting = true,
            }));

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.OperatorForce);

        Assert.Equal(DrainResult.Forced, outcome.OverallResult);
        Assert.Equal(1, outcome.ExecutionCyclesForceCancelledCount);
        Assert.Contains("instance-1", outcome.ForceCancelledInstanceIds);
        Assert.True(handle.CancellationToken.IsCancellationRequested);
        await InstanceStore.Received(1).SaveAsync(Arg.Is<WorkflowInstance>(i => i.SubStatus == WorkflowSubStatus.Interrupted && !i.IsExecuting), Arg.Any<CancellationToken>());
        await LogStore.Received(1).AddAsync(Arg.Is<Entities.WorkflowExecutionLogRecord>(r => r.EventName == WorkflowInterruptedPayload.WorkflowInterruptedEventName), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Force drain propagates fatal CTS callback exceptions wrapped in an aggregate")]
    public async Task ForceDrainPropagatesFatalCtsCallbackExceptions()
    {
        using var handle = new ExecutionCycleHandle(Guid.NewGuid(), "instance-fatal-callback", ingressSourceName: null, startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None);
        using var registration = handle.CancellationToken.Register(() => throw new OutOfMemoryException("fatal callback failure"));
        ExecutionCycleRegistry.ActiveCount.Returns(1);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle });
        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<WorkflowInstance?>(RunningInstance("instance-fatal-callback")));

        var sut = BuildSut();
        var exception = await Assert.ThrowsAsync<AggregateException>(() => sut.DrainAsync(DrainTrigger.OperatorForce).AsTask());

        Assert.Contains(exception.Flatten().InnerExceptions, inner => inner is OutOfMemoryException);
    }

    [Fact(DisplayName = "Persistence failure during drain produces Reason=PersistenceFailure in payload")]
    public async Task PersistenceFailureRecordsReason()
    {
        var handle = new ExecutionCycleHandle(Guid.NewGuid(), "instance-2", ingressSourceName: null, startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None);
        ExecutionCycleRegistry.ActiveCount.Returns(1);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle });
        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(new WorkflowInstance
            {
                Id = "instance-2",
                DefinitionId = "def-2",
                DefinitionVersionId = "ver-2",
                Version = 1,
                IsExecuting = true,
            }));
        InstanceStore.SaveAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("db unavailable"));

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.OperatorForce);

        Assert.Equal(DrainResult.Forced, outcome.OverallResult);
        await LogStore.Received(1).AddAsync(
            Arg.Is<Entities.WorkflowExecutionLogRecord>(r =>
                r.EventName == WorkflowInterruptedPayload.WorkflowInterruptedEventName
                && ((WorkflowInterruptedPayload)r.Payload!).Reason == WorkflowInterruptedPayload.ReasonPersistenceFailure),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Pre-cancel snapshot Finds never exceed MaxConcurrentPreCancelSnapshotFinds in flight")]
    public async Task SnapshotFindsAreCapped()
    {
        const int extra = 8;
        var cap = DrainOrchestrator.MaxConcurrentPreCancelSnapshotFinds;
        var count = cap + extra;
        var handles = Enumerable.Range(0, count)
            .Select(i => new ExecutionCycleHandle(Guid.NewGuid(), $"instance-capped-{i}", ingressSourceName: "http.trigger", startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None))
            .ToArray();
        ExecutionCycleRegistry.ActiveCount.Returns(count);
        ExecutionCycleRegistry.ListActiveCycles().Returns(handles);

        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var capReached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var inFlight = 0;
        var maxInFlight = 0;
        var maxLock = new object();

        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var id = ci.Arg<WorkflowInstanceFilter>().Id!;
                lock (seen)
                {
                    if (!seen.Add(id))
                        return new ValueTask<WorkflowInstance?>(RunningInstance(id));
                }

                return new ValueTask<WorkflowInstance?>(WaitForSnapshotSlotAsync(id));
            });

        async Task<WorkflowInstance?> WaitForSnapshotSlotAsync(string id)
        {
            var current = Interlocked.Increment(ref inFlight);
            lock (maxLock)
            {
                if (current > maxInFlight)
                    maxInFlight = current;
                if (maxInFlight >= cap)
                    capReached.TrySetResult();
            }

            try
            {
                await release.Task;
                return RunningInstance(id);
            }
            finally
            {
                Interlocked.Decrement(ref inFlight);
            }
        }

        var sut = BuildSut();
        var drainTask = sut.DrainAsync(DrainTrigger.OperatorForce).AsTask();

        await capReached.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(50);
        Assert.Equal(cap, maxInFlight);

        release.TrySetResult();

        var outcome = await drainTask.WaitAsync(TimeSpan.FromSeconds(8));
        Assert.Equal(DrainResult.Forced, outcome.OverallResult);
        Assert.Equal(count, outcome.ExecutionCyclesForceCancelledCount);
        foreach (var handle in handles)
            await InstanceStore.Received().SaveAsync(Arg.Is<WorkflowInstance>(i => i.Id == handle.WorkflowInstanceId && i.SubStatus == WorkflowSubStatus.Interrupted), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A null pre-cancel snapshot still promotes a later drain-induced Finished/Cancelled row")]
    public async Task NullSnapshotPromotesLaterCancelledInstance()
    {
        var handle = new ExecutionCycleHandle(Guid.NewGuid(), "instance-missing", ingressSourceName: "http.trigger", startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None);
        ExecutionCycleRegistry.ActiveCount.Returns(1);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle });

        var finds = 0;
        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (Interlocked.Increment(ref finds) == 1)
                    return new ValueTask<WorkflowInstance?>((WorkflowInstance?)null);

                return new ValueTask<WorkflowInstance?>(CancelledInstance("instance-missing"));
            });

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.OperatorForce);

        Assert.Equal(DrainResult.Forced, outcome.OverallResult);
        Assert.Equal(1, outcome.ExecutionCyclesForceCancelledCount);
        await InstanceStore.Received().SaveAsync(Arg.Is<WorkflowInstance>(i => i.Id == "instance-missing" && i.SubStatus == WorkflowSubStatus.Interrupted), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A confirmed Cancelled pre-cancel snapshot does not promote a later Finished/Cancelled row")]
    public async Task CancelledSnapshotDoesNotPromoteLaterCancelledInstance()
    {
        var handle = new ExecutionCycleHandle(Guid.NewGuid(), "instance-user-cancel", ingressSourceName: "http.trigger", startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None);
        ExecutionCycleRegistry.ActiveCount.Returns(1);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle });
        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<WorkflowInstance?>(CancelledInstance("instance-user-cancel")));

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.OperatorForce);

        Assert.Equal(DrainResult.Forced, outcome.OverallResult);
        Assert.Equal(1, outcome.ExecutionCyclesForceCancelledCount);
        await InstanceStore.DidNotReceive().SaveAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>());
        await LogStore.DidNotReceive().AddAsync(Arg.Any<Entities.WorkflowExecutionLogRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A null snapshot of an already-disposed handle does not promote a later Finished/Cancelled row")]
    public async Task DisposedHandleNullSnapshotDoesNotPromoteLaterCancelledInstance()
    {
        var handle = new ExecutionCycleHandle(Guid.NewGuid(), "instance-already-done", ingressSourceName: "http.trigger", startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None);
        handle.Dispose();
        ExecutionCycleRegistry.ActiveCount.Returns(1);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle });

        var finds = 0;
        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (Interlocked.Increment(ref finds) == 1)
                    return new ValueTask<WorkflowInstance?>((WorkflowInstance?)null);

                return new ValueTask<WorkflowInstance?>(CancelledInstance("instance-already-done"));
            });

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.OperatorForce);

        Assert.Equal(DrainResult.Forced, outcome.OverallResult);
        Assert.Equal(0, outcome.ExecutionCyclesForceCancelledCount);
        await InstanceStore.DidNotReceive().SaveAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>());
        await LogStore.DidNotReceive().AddAsync(Arg.Any<Entities.WorkflowExecutionLogRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A disposed handle with an executing snapshot is persisted as Interrupted")]
    public async Task DisposedHandleWithExecutingSnapshotIsPersisted()
    {
        var handle = new ExecutionCycleHandle(Guid.NewGuid(), "instance-disposed-at-checkpoint", ingressSourceName: "http.trigger", startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None);
        handle.Dispose();
        ExecutionCycleRegistry.ActiveCount.Returns(1);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle });
        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<WorkflowInstance?>(RunningInstance("instance-disposed-at-checkpoint")));

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.OperatorForce);

        Assert.Equal(DrainResult.Forced, outcome.OverallResult);
        Assert.Equal(0, outcome.ExecutionCyclesForceCancelledCount);
        await InstanceStore.Received(1).SaveAsync(
            Arg.Is<WorkflowInstance>(i => i.Id == "instance-disposed-at-checkpoint" && i.SubStatus == WorkflowSubStatus.Interrupted && !i.IsExecuting),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A disposed handle with an active snapshot is not persisted after the row suspends")]
    public async Task DisposedHandleWithActiveSnapshotDoesNotPersistLaterSuspendedInstance()
    {
        var handle = new ExecutionCycleHandle(Guid.NewGuid(), "instance-suspended-after-snapshot", ingressSourceName: "http.trigger", startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None);
        handle.Dispose();
        ExecutionCycleRegistry.ActiveCount.Returns(1);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle });

        var finds = 0;
        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ => Interlocked.Increment(ref finds) == 1
                ? new ValueTask<WorkflowInstance?>(RunningInstance("instance-suspended-after-snapshot"))
                : new ValueTask<WorkflowInstance?>(SuspendedInstance("instance-suspended-after-snapshot")));

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.OperatorForce);

        Assert.Equal(DrainResult.Forced, outcome.OverallResult);
        Assert.Equal(0, outcome.ExecutionCyclesForceCancelledCount);
        await InstanceStore.DidNotReceive().SaveAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>());
        await LogStore.DidNotReceive().AddAsync(Arg.Any<Entities.WorkflowExecutionLogRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A disposed handle with a suspended snapshot is not persisted as Interrupted")]
    public async Task DisposedHandleWithSuspendedSnapshotIsNotPersisted()
    {
        var handle = new ExecutionCycleHandle(Guid.NewGuid(), "instance-suspended", ingressSourceName: "http.trigger", startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None);
        handle.Dispose();
        ExecutionCycleRegistry.ActiveCount.Returns(1);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle });
        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<WorkflowInstance?>(SuspendedInstance("instance-suspended")));

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.OperatorForce);

        Assert.Equal(DrainResult.Forced, outcome.OverallResult);
        Assert.Equal(0, outcome.ExecutionCyclesForceCancelledCount);
        await InstanceStore.DidNotReceive().SaveAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "An active snapshot disposed after a failed cancel is recovered after settling")]
    public async Task ActiveSnapshotDisposedAfterFailedCancelIsRecoveredAfterSettling()
    {
        var targetCallbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseTargetCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var target = new ExecutionCycleHandle(
            Guid.NewGuid(),
            "instance-disposed-during-settle",
            ingressSourceName: "http.trigger",
            startedAt: DateTimeOffset.UtcNow,
            linkedToken: CancellationToken.None,
            cancelCallback: () =>
            {
                targetCallbackEntered.SetResult();
                releaseTargetCallback.Task.GetAwaiter().GetResult();
            });
        var blockerCallbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseBlockerCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var blocker = new ExecutionCycleHandle(
            Guid.NewGuid(),
            "instance-phase-a-blocker",
            ingressSourceName: "http.trigger",
            startedAt: DateTimeOffset.UtcNow,
            linkedToken: CancellationToken.None,
            cancelCallback: () =>
            {
                blockerCallbackEntered.SetResult();
                releaseBlockerCallback.Task.GetAwaiter().GetResult();
            });
        var preCancelTask = Task.Run(target.TryCancel);
        Task<DrainOutcome>? drainTask = null;

        try
        {
            await targetCallbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

            ExecutionCycleRegistry.ActiveCount.Returns(2);
            ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { target, blocker });
            InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
                .Returns(ci => new ValueTask<WorkflowInstance?>(RunningInstance(ci.Arg<WorkflowInstanceFilter>().Id!)));

            var sut = BuildSut();
            drainTask = Task.Run(async () => await sut.DrainAsync(DrainTrigger.OperatorForce));
            await blockerCallbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(target.Disposed.IsCompleted);

            releaseBlockerCallback.TrySetResult();
            await Task.Yield();
            target.Dispose();
            releaseTargetCallback.TrySetResult();

            Assert.False(await preCancelTask.WaitAsync(TimeSpan.FromSeconds(5)));
            var outcome = await drainTask.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(DrainResult.Forced, outcome.OverallResult);
            Assert.Equal(1, outcome.ExecutionCyclesForceCancelledCount);
            await InstanceStore.Received(1).SaveAsync(
                Arg.Is<WorkflowInstance>(i => i.Id == target.WorkflowInstanceId && i.SubStatus == WorkflowSubStatus.Interrupted && !i.IsExecuting),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            // Always release synchronous callback gates so an assertion or timeout cannot strand the test host.
            releaseBlockerCallback.TrySetResult();
            releaseTargetCallback.TrySetResult();

            await ObserveCleanupAsync(preCancelTask);

            if (drainTask is not null)
                await ObserveCleanupAsync(drainTask);
        }
    }

    [Fact(DisplayName = "A canceled drain still observes deferred disposal of a failed-cancel candidate")]
    public async Task CanceledDrainObservesDeferredCandidateDisposal()
    {
        using var drainCts = new CancellationTokenSource();
        var disposalEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseDisposal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handle = new ExecutionCycleHandle(
            Guid.NewGuid(),
            "instance-canceled-drain-disposal",
            ingressSourceName: "http.trigger",
            startedAt: DateTimeOffset.UtcNow,
            linkedToken: CancellationToken.None,
            onDisposed: _ =>
            {
                disposalEntered.SetResult();
                releaseDisposal.Task.GetAwaiter().GetResult();
            });
        var disposeTask = Task.Run(handle.Dispose);
        var blockerCallbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseBlockerCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var blocker = new ExecutionCycleHandle(
            Guid.NewGuid(),
            "instance-canceled-drain-blocker",
            ingressSourceName: "http.trigger",
            startedAt: DateTimeOffset.UtcNow,
            linkedToken: CancellationToken.None,
            cancelCallback: () =>
            {
                blockerCallbackEntered.SetResult();
                releaseBlockerCallback.Task.GetAwaiter().GetResult();
            });
        ExecutionCycleRegistry.ActiveCount.Returns(2);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle, blocker });
        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(ci => new ValueTask<WorkflowInstance?>(RunningInstance(ci.Arg<WorkflowInstanceFilter>().Id!)));

        Task<DrainOutcome>? drainTask = null;
        try
        {
            await disposalEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var sut = BuildSut();
            // Force-cancel invokes synchronous callbacks in Phase A. Keep the test thread available to
            // release the blocker and cancel the drain while that callback is intentionally suspended.
            drainTask = Task.Run(async () => await sut.DrainAsync(DrainTrigger.OperatorForce, drainCts.Token));
            await blockerCallbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await drainCts.CancelAsync();
            releaseBlockerCallback.TrySetResult();

            await Assert.ThrowsAsync<TimeoutException>(() => drainTask.WaitAsync(TimeSpan.FromSeconds(1)));
            releaseDisposal.TrySetResult();

            var outcome = await drainTask.WaitAsync(TimeSpan.FromSeconds(5));
            await disposeTask.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(DrainResult.Forced, outcome.OverallResult);
            Assert.Equal(1, outcome.ExecutionCyclesForceCancelledCount);
            Assert.DoesNotContain(handle.WorkflowInstanceId, outcome.ForceCancelledInstanceIds);
            await InstanceStore.Received(1).SaveAsync(
                Arg.Is<WorkflowInstance>(i => i.Id == handle.WorkflowInstanceId && i.SubStatus == WorkflowSubStatus.Interrupted && !i.IsExecuting),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            releaseBlockerCallback.TrySetResult();
            releaseDisposal.TrySetResult();
            await ObserveCleanupAsync(disposeTask);

            if (drainTask is not null)
                await ObserveCleanupAsync(drainTask);
        }
    }

    [Fact(DisplayName = "A disposed handle is not persisted as Interrupted when its later row is still running")]
    public async Task DisposedHandleDoesNotPersistLaterRunningInstance()
    {
        var handle = new ExecutionCycleHandle(Guid.NewGuid(), "instance-already-running", ingressSourceName: "http.trigger", startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None);
        handle.Dispose();
        ExecutionCycleRegistry.ActiveCount.Returns(1);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle });

        var finds = 0;
        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (Interlocked.Increment(ref finds) == 1)
                    return new ValueTask<WorkflowInstance?>((WorkflowInstance?)null);

                return new ValueTask<WorkflowInstance?>(RunningInstance("instance-already-running"));
            });

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.OperatorForce);

        Assert.Equal(DrainResult.Forced, outcome.OverallResult);
        Assert.Equal(0, outcome.ExecutionCyclesForceCancelledCount);
        await InstanceStore.DidNotReceive().SaveAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>());
        await LogStore.DidNotReceive().AddAsync(Arg.Any<Entities.WorkflowExecutionLogRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A cycle disposed during cancellation is not counted or persisted as drain-cancelled")]
    public async Task CycleDisposedDuringCancellationIsNotCountedOrPersisted()
    {
        var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handle = new ExecutionCycleHandle(
            Guid.NewGuid(),
            "instance-completed-during-cancel",
            ingressSourceName: "http.trigger",
            startedAt: DateTimeOffset.UtcNow,
            linkedToken: CancellationToken.None,
            cancelCallback: () =>
            {
                callbackEntered.SetResult();
                releaseCallback.Task.GetAwaiter().GetResult();
            });
        ExecutionCycleRegistry.ActiveCount.Returns(1);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle });

        var finds = 0;
        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (Interlocked.Increment(ref finds) == 1)
                    return new ValueTask<WorkflowInstance?>((WorkflowInstance?)null);

                return new ValueTask<WorkflowInstance?>(RunningInstance("instance-completed-during-cancel"));
            });

        var sut = BuildSut();
        var drainTask = Task.Run(async () => await sut.DrainAsync(DrainTrigger.OperatorForce));
        var testFailed = false;

        try
        {
            await callbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

            handle.Dispose();
            releaseCallback.TrySetResult();

            var outcome = await drainTask.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(DrainResult.Forced, outcome.OverallResult);
            Assert.Equal(0, outcome.ExecutionCyclesForceCancelledCount);
            await InstanceStore.DidNotReceive().SaveAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>());
            await LogStore.DidNotReceive().AddAsync(Arg.Any<Entities.WorkflowExecutionLogRecord>(), Arg.Any<CancellationToken>());
        }
        catch
        {
            testFailed = true;
            throw;
        }
        finally
        {
            releaseCallback.TrySetResult();
            handle.Dispose();
            await ObserveCleanupAsync(drainTask, testFailed);
        }
    }

    [Fact(DisplayName = "Waiting for a snapshot slot does not burn the per-Find 250ms budget")]
    public async Task SnapshotQueueWaitDoesNotExcludeLaterFinds()
    {
        var cap = DrainOrchestrator.MaxConcurrentPreCancelSnapshotFinds;
        const int extra = 4;
        var count = cap + extra;
        var handles = Enumerable.Range(0, count)
            .Select(i => new ExecutionCycleHandle(Guid.NewGuid(), $"instance-queued-{i}", ingressSourceName: "http.trigger", startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None))
            .ToArray();
        ExecutionCycleRegistry.ActiveCount.Returns(count);
        ExecutionCycleRegistry.ListActiveCycles().Returns(handles);

        var hang = new TaskCompletionSource<WorkflowInstance?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var stalledIds = new HashSet<string>(StringComparer.Ordinal);
        var promotedIds = new HashSet<string>(StringComparer.Ordinal);
        var snapshotFinds = 0;

        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var id = ci.Arg<WorkflowInstanceFilter>().Id!;
                lock (seen)
                {
                    // Phase C: runner committed Finished/Cancelled after force-cancel.
                    // Only ids that snapshot successfully join drainInduced and may promote.
                    if (!seen.Add(id))
                        return new ValueTask<WorkflowInstance?>(CancelledInstance(id));
                }

                if (Interlocked.Increment(ref snapshotFinds) <= cap)
                {
                    lock (stalledIds)
                        stalledIds.Add(id);
                    return new ValueTask<WorkflowInstance?>(hang.Task);
                }

                lock (promotedIds)
                    promotedIds.Add(id);
                return new ValueTask<WorkflowInstance?>(RunningInstance(id));
            });

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.OperatorForce).AsTask().WaitAsync(TimeSpan.FromSeconds(8));

        Assert.Equal(DrainResult.Forced, outcome.OverallResult);
        Assert.Equal(count, outcome.ExecutionCyclesForceCancelledCount);
        Assert.Equal(cap, stalledIds.Count);
        Assert.Equal(extra, promotedIds.Count);
        Assert.False(hang.Task.IsCompleted);

        foreach (var id in stalledIds)
            await InstanceStore.DidNotReceive().SaveAsync(Arg.Is<WorkflowInstance>(i => i.Id == id), Arg.Any<CancellationToken>());
        foreach (var id in promotedIds)
            await InstanceStore.Received().SaveAsync(Arg.Is<WorkflowInstance>(i => i.Id == id && i.SubStatus == WorkflowSubStatus.Interrupted), Arg.Any<CancellationToken>());
    }

    private static WorkflowInstance RunningInstance(string id) => new()
    {
        Id = id,
        DefinitionId = "def-1",
        DefinitionVersionId = "ver-1",
        Version = 1,
        Status = WorkflowStatus.Running,
        SubStatus = WorkflowSubStatus.Executing,
        IsExecuting = true,
    };

    private static async Task ObserveCleanupAsync(Task task, bool preserveTestFailure = false)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (Exception) when (preserveTestFailure)
        {
            // Preserve the original assertion/timeout while observing the cleanup task.
        }
        catch (TimeoutException)
        {
            // Preserve the original assertion/timeout while observing the cleanup task.
        }
        catch (OperationCanceledException)
        {
            // Preserve the original assertion/timeout while observing the cleanup task.
        }
    }

    private static WorkflowInstance SuspendedInstance(string id) => new()
    {
        Id = id,
        DefinitionId = "def-1",
        DefinitionVersionId = "ver-1",
        Version = 1,
        Status = WorkflowStatus.Running,
        SubStatus = WorkflowSubStatus.Suspended,
        IsExecuting = false,
    };

    private static WorkflowInstance CancelledInstance(string id) => new()
    {
        Id = id,
        DefinitionId = "def-1",
        DefinitionVersionId = "ver-1",
        Version = 1,
        Status = WorkflowStatus.Finished,
        SubStatus = WorkflowSubStatus.Cancelled,
        IsExecuting = false,
    };

    [Fact(DisplayName = "Second non-force drain in same generation throws InvalidOperationException")]
    public async Task SecondNonForceDrainThrows()
    {
        var sut = BuildSut();
        await sut.DrainAsync(DrainTrigger.HostStopSignal);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.DrainAsync(DrainTrigger.HostStopSignal));
    }

    [Fact(DisplayName = "Operator force after a completed drain returns the previous outcome with WasCached=true")]
    public async Task OperatorForceAfterPreviousReturnsCachedOutcome()
    {
        var sut = BuildSut();
        var first = await sut.DrainAsync(DrainTrigger.HostStopSignal);
        var second = await sut.DrainAsync(DrainTrigger.OperatorForce);

        Assert.False(first.WasCached, "First (fresh) drain must not be flagged as cached.");
        Assert.True(second.WasCached, "Second (force-after-completed) drain must be flagged as cached so the admin endpoint skips audit publishing.");
        // Same payload modulo the WasCached flag.
        Assert.Equal(first with { WasCached = true }, second);
    }

    [Fact(DisplayName = "Host stop drain swallows ObjectDisposedException after shutdown cancellation")]
    public async Task HostStopDrainSwallowsObjectDisposedExceptionAfterCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var orchestrator = Substitute.For<IDrainOrchestrator>();
        orchestrator.DrainAsync(DrainTrigger.HostStopSignal, cts.Token).Returns(_ => ThrowObjectDisposedAsync());
        var hostedService = new DrainOrchestratorHostedService(orchestrator, Substitute.For<ILogger<DrainOrchestratorHostedService>>());

        await hostedService.StopAsync(cts.Token);
    }

    [Fact(DisplayName = "Host stop drain propagates ObjectDisposedException before shutdown cancellation")]
    public async Task HostStopDrainPropagatesObjectDisposedExceptionBeforeCancellation()
    {
        var orchestrator = Substitute.For<IDrainOrchestrator>();
        orchestrator.DrainAsync(DrainTrigger.HostStopSignal, CancellationToken.None).Returns(_ => ThrowObjectDisposedAsync());
        var hostedService = new DrainOrchestratorHostedService(orchestrator, Substitute.For<ILogger<DrainOrchestratorHostedService>>());

        await Assert.ThrowsAsync<ObjectDisposedException>(() => hostedService.StopAsync(CancellationToken.None));
    }

    private static async ValueTask<DrainOutcome> ThrowObjectDisposedAsync()
    {
        await Task.Yield();
        throw new ObjectDisposedException("drain dependency");
    }
}
