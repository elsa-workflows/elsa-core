using System.Diagnostics;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.HostedServices;
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
        InstanceStore.TryMarkInterruptedAsync("instance-1", Arg.Any<CancellationToken>(), false).Returns(new ValueTask<bool>(true));

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.OperatorForce);

        Assert.Equal(DrainResult.Forced, outcome.OverallResult);
        Assert.Equal(1, outcome.ExecutionCyclesForceCancelledCount);
        Assert.Contains("instance-1", outcome.ForceCancelledInstanceIds);
        Assert.True(handle.CancellationToken.IsCancellationRequested);
        await InstanceStore.Received(1).TryMarkInterruptedAsync("instance-1", Arg.Any<CancellationToken>(), false);
        await InstanceStore.DidNotReceive().SaveAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>());
        await LogStore.Received(1).AddAsync(Arg.Is<Entities.WorkflowExecutionLogRecord>(r => r.EventName == WorkflowInterruptedPayload.WorkflowInterruptedEventName), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Force-cancel skips Interrupted persist when the instance is already Finished")]
    public async Task SkipsInterruptedPersistForFinishedInstance()
    {
        var handle = new ExecutionCycleHandle(Guid.NewGuid(), "instance-finished", ingressSourceName: "http.trigger", startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None);
        ExecutionCycleRegistry.ActiveCount.Returns(1);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle });
        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(new WorkflowInstance
            {
                Id = "instance-finished",
                DefinitionId = "def-1",
                DefinitionVersionId = "ver-1",
                Version = 1,
                Status = WorkflowStatus.Finished,
                SubStatus = WorkflowSubStatus.Finished,
                IsExecuting = false,
            }));

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.OperatorForce);

        Assert.Equal(DrainResult.Forced, outcome.OverallResult);
        Assert.Equal(1, outcome.ExecutionCyclesForceCancelledCount);
        Assert.Contains("instance-finished", outcome.ForceCancelledInstanceIds);
        await InstanceStore.DidNotReceive().SaveAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>());
        await InstanceStore.DidNotReceive().TryMarkInterruptedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
        await LogStore.DidNotReceive().AddAsync(Arg.Any<Entities.WorkflowExecutionLogRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Force-cancel persists Interrupted when the runner commits Finished/Cancelled after drain cancel")]
    public async Task PersistsInterruptedForCancelledInstance()
    {
        var handle = new ExecutionCycleHandle(Guid.NewGuid(), "instance-cancelled", ingressSourceName: "http.trigger", startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None);
        ExecutionCycleRegistry.ActiveCount.Returns(1);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle });
        var running = new WorkflowInstance
        {
            Id = "instance-cancelled",
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Version = 1,
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Executing,
            IsExecuting = true,
        };
        var cancelled = new WorkflowInstance
        {
            Id = "instance-cancelled",
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Version = 1,
            Status = WorkflowStatus.Finished,
            SubStatus = WorkflowSubStatus.Cancelled,
            IsExecuting = false,
        };
        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<WorkflowInstance?>(running), _ => new ValueTask<WorkflowInstance?>(cancelled));
        InstanceStore.TryMarkInterruptedAsync("instance-cancelled", Arg.Any<CancellationToken>(), true).Returns(new ValueTask<bool>(true));

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.OperatorForce);

        Assert.Equal(DrainResult.Forced, outcome.OverallResult);
        Assert.Equal(1, outcome.ExecutionCyclesForceCancelledCount);
        await InstanceStore.Received(1).TryMarkInterruptedAsync("instance-cancelled", Arg.Any<CancellationToken>(), true);
        await LogStore.Received(1).AddAsync(Arg.Is<Entities.WorkflowExecutionLogRecord>(r => r.EventName == WorkflowInterruptedPayload.WorkflowInterruptedEventName), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Force-cancel does not promote a user cancellation that was already Cancelled when drain snapshotted the live cycle")]
    public async Task SkipsInterruptedPersistForAlreadyUserCancelledInstance()
    {
        var handle = new ExecutionCycleHandle(Guid.NewGuid(), "instance-user-cancelled", ingressSourceName: "http.trigger", startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None);
        ExecutionCycleRegistry.ActiveCount.Returns(1);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle });
        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(new WorkflowInstance
            {
                Id = "instance-user-cancelled",
                DefinitionId = "def-1",
                DefinitionVersionId = "ver-1",
                Version = 1,
                Status = WorkflowStatus.Finished,
                SubStatus = WorkflowSubStatus.Cancelled,
                IsExecuting = false,
            }));

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.OperatorForce);

        Assert.Equal(DrainResult.Forced, outcome.OverallResult);
        Assert.Equal(1, outcome.ExecutionCyclesForceCancelledCount);
        await InstanceStore.DidNotReceive().TryMarkInterruptedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
        await LogStore.DidNotReceive().AddAsync(Arg.Any<Entities.WorkflowExecutionLogRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Stalled pre-cancel FindAsync does not prevent force-cancel or drain completion")]
    public async Task StalledSnapshotDoesNotBlockForceCancel()
    {
        var handle = new ExecutionCycleHandle(Guid.NewGuid(), "instance-stalled", ingressSourceName: "http.trigger", startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None);
        ExecutionCycleRegistry.ActiveCount.Returns(1);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle });
        var stalled = new TaskCompletionSource<WorkflowInstance?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finds = 0;
        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (Interlocked.Increment(ref finds) == 1)
                    return new ValueTask<WorkflowInstance?>(stalled.Task);

                return new ValueTask<WorkflowInstance?>(new WorkflowInstance
                {
                    Id = "instance-stalled",
                    DefinitionId = "def-1",
                    DefinitionVersionId = "ver-1",
                    Version = 1,
                    Status = WorkflowStatus.Finished,
                    SubStatus = WorkflowSubStatus.Cancelled,
                    IsExecuting = false,
                });
            });

        var sut = BuildSut();
        var drainTask = sut.DrainAsync(DrainTrigger.OperatorForce).AsTask();

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(1);
        while (!handle.CancellationToken.IsCancellationRequested && DateTime.UtcNow < deadline)
            await Task.Delay(20);

        Assert.True(handle.CancellationToken.IsCancellationRequested, "Force-cancel must run even while the snapshot FindAsync is still stalled.");
        Assert.False(stalled.Task.IsCompleted);

        var outcome = await drainTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(DrainResult.Forced, outcome.OverallResult);
        Assert.Equal(1, outcome.ExecutionCyclesForceCancelledCount);
        Assert.Contains("instance-stalled", outcome.ForceCancelledInstanceIds);
        await InstanceStore.DidNotReceive().TryMarkInterruptedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
    }

    [Fact(DisplayName = "A stalled first snapshot does not exclude a later instance from drain-induced Interrupted promote")]
    public async Task IndependentSnapshotTimeoutDoesNotStarveLaterFinds()
    {
        var stalledHandle = new ExecutionCycleHandle(Guid.NewGuid(), "instance-stalled", ingressSourceName: "http.trigger", startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None);
        var recoveredHandle = new ExecutionCycleHandle(Guid.NewGuid(), "instance-recovered", ingressSourceName: "http.trigger", startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None);
        ExecutionCycleRegistry.ActiveCount.Returns(2);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { stalledHandle, recoveredHandle });

        var stalled = new TaskCompletionSource<WorkflowInstance?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stalledFinds = 0;
        InstanceStore.FindAsync(Arg.Is<WorkflowInstanceFilter>(f => f.Id == "instance-stalled"), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (Interlocked.Increment(ref stalledFinds) == 1)
                    return new ValueTask<WorkflowInstance?>(stalled.Task);

                return new ValueTask<WorkflowInstance?>(new WorkflowInstance
                {
                    Id = "instance-stalled",
                    DefinitionId = "def-1",
                    DefinitionVersionId = "ver-1",
                    Version = 1,
                    Status = WorkflowStatus.Finished,
                    SubStatus = WorkflowSubStatus.Cancelled,
                    IsExecuting = false,
                });
            });
        InstanceStore.FindAsync(Arg.Is<WorkflowInstanceFilter>(f => f.Id == "instance-recovered"), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<WorkflowInstance?>(new WorkflowInstance
            {
                Id = "instance-recovered",
                DefinitionId = "def-1",
                DefinitionVersionId = "ver-1",
                Version = 1,
                Status = WorkflowStatus.Running,
                SubStatus = WorkflowSubStatus.Executing,
                IsExecuting = true,
            }));
        InstanceStore.TryMarkInterruptedAsync("instance-recovered", Arg.Any<CancellationToken>(), false).Returns(new ValueTask<bool>(true));

        var sut = BuildSut();
        var drainTask = sut.DrainAsync(DrainTrigger.OperatorForce).AsTask();

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while ((!stalledHandle.CancellationToken.IsCancellationRequested || !recoveredHandle.CancellationToken.IsCancellationRequested) && DateTime.UtcNow < deadline)
            await Task.Delay(20);

        Assert.True(stalledHandle.CancellationToken.IsCancellationRequested);
        Assert.True(recoveredHandle.CancellationToken.IsCancellationRequested);
        Assert.False(stalled.Task.IsCompleted);

        var outcome = await drainTask.WaitAsync(TimeSpan.FromSeconds(8));
        Assert.Equal(DrainResult.Forced, outcome.OverallResult);
        Assert.Equal(2, outcome.ExecutionCyclesForceCancelledCount);
        await InstanceStore.DidNotReceive().TryMarkInterruptedAsync("instance-stalled", Arg.Any<CancellationToken>(), Arg.Any<bool>());
        await InstanceStore.Received().TryMarkInterruptedAsync("instance-recovered", Arg.Any<CancellationToken>(), false);
    }

    [Fact(DisplayName = "Many stalled pre-cancel Finds do not serialize Phase A Cancel behind N snapshot timeouts")]
    public async Task ParallelStalledSnapshotsDoNotSerializeForceCancel()
    {
        const int count = 8;
        var handles = Enumerable.Range(0, count)
            .Select(i => new ExecutionCycleHandle(Guid.NewGuid(), $"instance-stalled-{i}", ingressSourceName: "http.trigger", startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None))
            .ToArray();
        ExecutionCycleRegistry.ActiveCount.Returns(count);
        ExecutionCycleRegistry.ListActiveCycles().Returns(handles);

        var hang = new TaskCompletionSource<WorkflowInstance?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var id = ci.Arg<WorkflowInstanceFilter>().Id!;
                lock (seen)
                {
                    if (seen.Add(id))
                        return new ValueTask<WorkflowInstance?>(hang.Task);
                }

                return new ValueTask<WorkflowInstance?>(new WorkflowInstance
                {
                    Id = id,
                    DefinitionId = "def-1",
                    DefinitionVersionId = "ver-1",
                    Version = 1,
                    Status = WorkflowStatus.Finished,
                    SubStatus = WorkflowSubStatus.Cancelled,
                    IsExecuting = false,
                });
            });

        var sut = BuildSut();
        var started = Stopwatch.StartNew();
        var drainTask = sut.DrainAsync(DrainTrigger.OperatorForce).AsTask();

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(1);
        while (handles.Any(h => !h.CancellationToken.IsCancellationRequested) && DateTime.UtcNow < deadline)
            await Task.Delay(10);

        var cancelElapsed = started.Elapsed;
        Assert.All(handles, h => Assert.True(h.CancellationToken.IsCancellationRequested));
        Assert.True(
            cancelElapsed < TimeSpan.FromSeconds(1),
            $"Phase A Cancel waited {cancelElapsed}; concurrent snapshots must finish in ~one 250ms window, not {count}×250ms.");
        Assert.False(hang.Task.IsCompleted);

        var outcome = await drainTask.WaitAsync(TimeSpan.FromSeconds(8));
        Assert.Equal(DrainResult.Forced, outcome.OverallResult);
        Assert.Equal(count, outcome.ExecutionCyclesForceCancelledCount);
        await InstanceStore.DidNotReceive().TryMarkInterruptedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
    }

    [Fact(DisplayName = "Force-cancel skips Interrupted persist when TryMarkInterrupted loses the terminal race")]
    public async Task SkipsInterruptedPersistWhenMarkLosesTerminalRace()
    {
        var handle = new ExecutionCycleHandle(Guid.NewGuid(), "instance-raced", ingressSourceName: "http.trigger", startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None);
        ExecutionCycleRegistry.ActiveCount.Returns(1);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle });
        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(new WorkflowInstance
            {
                Id = "instance-raced",
                DefinitionId = "def-1",
                DefinitionVersionId = "ver-1",
                Version = 1,
                Status = WorkflowStatus.Running,
                IsExecuting = true,
            }));
        InstanceStore.TryMarkInterruptedAsync("instance-raced", Arg.Any<CancellationToken>(), false).Returns(new ValueTask<bool>(false));

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.OperatorForce);

        Assert.Equal(DrainResult.Forced, outcome.OverallResult);
        await InstanceStore.Received(1).TryMarkInterruptedAsync("instance-raced", Arg.Any<CancellationToken>(), false);
        await InstanceStore.DidNotReceive().SaveAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>());
        await LogStore.DidNotReceive().AddAsync(Arg.Any<Entities.WorkflowExecutionLogRecord>(), Arg.Any<CancellationToken>());
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
        InstanceStore.TryMarkInterruptedAsync("instance-2", Arg.Any<CancellationToken>(), false)
            .Returns(_ => ValueTask.FromException<bool>(new InvalidOperationException("db unavailable")));

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.OperatorForce);

        Assert.Equal(DrainResult.Forced, outcome.OverallResult);
        await LogStore.Received(1).AddAsync(
            Arg.Is<Entities.WorkflowExecutionLogRecord>(r =>
                r.EventName == WorkflowInterruptedPayload.WorkflowInterruptedEventName
                && ((WorkflowInterruptedPayload)r.Payload!).Reason == WorkflowInterruptedPayload.ReasonPersistenceFailure),
            Arg.Any<CancellationToken>());
    }

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
