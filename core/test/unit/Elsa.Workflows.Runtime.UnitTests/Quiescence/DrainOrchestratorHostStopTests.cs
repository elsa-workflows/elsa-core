using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.State;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Quiescence;

/// <summary>
/// A drain whose deadline passes with <c>InterruptOnHostStop</c> on, for a cycle that runs in a named tenant.
/// </summary>
public class DrainOrchestratorHostStopTests : DrainOrchestratorTestsBase
{
    private static readonly Tenant TenantA = new() { Id = "tenant-a", Name = "tenant-a" };
    private bool _cancelCallbackInvoked;

    public DrainOrchestratorHostStopTests()
    {
        Options.InterruptOnHostStop = true;

        // Every poll of the wait phase moves the clock on, so the deadline passes.
        ExecutionCycleRegistry.ActiveCount.Returns(_ =>
        {
            AdvanceTime(TimeSpan.FromSeconds(1));
            return 1;
        });
    }

    [Fact(DisplayName = "An interrupted cycle is not cancelled, and its checkpoint is marked Interrupted under the cycle's tenant")]
    public async Task CheckpointIsMarkedWithoutCancelling()
    {
        var handle = Handle("checkpoint");
        StubInstance(Instance("checkpoint", new() { SubStatus = WorkflowSubStatus.Executing, ActivityExecutionContexts = { new() { IsExecuting = true } } }));
        string? markedUnder = null;
        InstanceStore.TryMarkInterruptedAsync("checkpoint", Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(_ =>
        {
            markedUnder = TenantAccessor.Tenant?.Id;
            return new ValueTask<bool>(true);
        });

        var outcome = await BuildSut().DrainAsync(DrainTrigger.HostStopSignal);

        Assert.Equal((DrainResult.DeadlineExceeded, 1, TenantA.Id), (outcome.OverallResult, outcome.ExecutionCyclesForceCancelledCount, markedUnder));
        Assert.Equal(["checkpoint"], outcome.ForceCancelledInstanceIds);
        Assert.False(_cancelCallbackInvoked);
        Assert.False(handle.CancellationToken.IsCancellationRequested);
        Assert.True(handle.IsInterrupted);
        await LogStore.Received(1).AddAsync(Arg.Is<WorkflowExecutionLogRecord>(x => x.EventName == WorkflowInterruptedPayload.WorkflowInterruptedEventName), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "An interrupted resume that never committed is queued again, and its suspension is left as it is")]
    public async Task UncommittedResumeIsQueuedAgain()
    {
        var handle = Handle("resume");
        handle.ResumedBookmark = new("bookmark-1", "Elsa.Event", "hash", null, "activity", "node", null, DateTimeOffset.UtcNow);
        handle.ResumeInput = new Dictionary<string, object> { ["payload"] = 42 };
        StubInstance(Instance("resume", new() { SubStatus = WorkflowSubStatus.Suspended }));

        await BuildSut().DrainAsync(DrainTrigger.HostStopSignal);

        await BookmarkQueueStore.Received(1).AddAsync(
            Arg.Is<BookmarkQueueItem>(x => x.WorkflowInstanceId == "resume" && x.BookmarkId == "bookmark-1" && x.TenantId == TenantA.Id && Equals(x.Options!.Input!["payload"], 42)),
            Arg.Any<CancellationToken>());
        await InstanceStore.DidNotReceive().TryMarkInterruptedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
        Assert.False(_cancelCallbackInvoked);
    }

    [Fact(DisplayName = "An interrupted fresh run without a row is persisted as not started")]
    public async Task UncommittedFreshRunIsPersistedAsPending()
    {
        var handle = Handle("fresh");
        handle.CreatePendingInstance = _ => new()
        {
            Id = "fresh",
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Pending,
            WorkflowState = new() { Id = "fresh", Status = WorkflowStatus.Running, SubStatus = WorkflowSubStatus.Pending }
        };
        string? addedUnder = null;
        InstanceStore.When(x => x.AddAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>())).Do(_ => addedUnder = TenantAccessor.Tenant?.Id);

        await BuildSut().DrainAsync(DrainTrigger.HostStopSignal);

        await InstanceStore.Received(1).AddAsync(
            Arg.Is<WorkflowInstance>(x => x.Id == "fresh" && x.Status == WorkflowStatus.Running && x.SubStatus == WorkflowSubStatus.Interrupted && x.WorkflowState.SubStatus == WorkflowSubStatus.Pending),
            Arg.Any<CancellationToken>());
        Assert.Equal(TenantA.Id, addedUnder);
    }

    [Theory(DisplayName = "Operator force, and a host stop without InterruptOnHostStop, still cancel")]
    [InlineData(DrainTrigger.OperatorForce, true)]
    [InlineData(DrainTrigger.HostStopSignal, false)]
    public async Task OtherDrainsStillCancel(DrainTrigger trigger, bool interruptOnHostStop)
    {
        Options.InterruptOnHostStop = interruptOnHostStop;
        var handle = Handle("cancelled");
        StubInstance(Instance("cancelled", new() { SubStatus = WorkflowSubStatus.Executing, ActivityExecutionContexts = { new() { IsExecuting = true } } }));

        var outcome = await BuildSut().DrainAsync(trigger);

        Assert.Equal(1, outcome.ExecutionCyclesForceCancelledCount);
        Assert.True(_cancelCallbackInvoked);
        Assert.True(handle.CancellationToken.IsCancellationRequested);
        Assert.False(handle.IsInterrupted);
    }

    [Fact(DisplayName = "Interrupting a handle neither cancels it nor lets it be cancelled afterwards")]
    public void TryInterruptDoesNotCancel()
    {
        using var handle = Handle("instance");

        Assert.True(handle.TryInterrupt());
        Assert.False(handle.TryInterrupt());
        Assert.False(handle.TryCancel());
        Assert.False(_cancelCallbackInvoked);
        Assert.False(handle.CancellationToken.IsCancellationRequested);
    }

    private ExecutionCycleHandle Handle(string instanceId)
    {
        var handle = new ExecutionCycleHandle(Guid.NewGuid(), instanceId, ingressSourceName: null, startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None, cancelCallback: () => _cancelCallbackInvoked = true)
        {
            Tenant = TenantA
        };
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle });
        return handle;
    }

    // Visible only under the cycle's tenant, as the tenant query filter does.
    private void StubInstance(WorkflowInstance instance) =>
        InstanceStore.FindAsync(Arg.Is<WorkflowInstanceFilter>(f => f.Id == instance.Id), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<WorkflowInstance?>(TenantAccessor.Tenant?.Id == TenantA.Id ? instance : null));

    private static WorkflowInstance Instance(string id, WorkflowState state)
    {
        state.Id = id;
        state.Status = WorkflowStatus.Running;
        return new()
        {
            Id = id,
            TenantId = TenantA.Id,
            DefinitionId = "def",
            DefinitionVersionId = "ver",
            Version = 1,
            Status = WorkflowStatus.Running,
            SubStatus = state.SubStatus,
            WorkflowState = state
        };
    }
}
