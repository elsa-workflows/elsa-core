using Elsa.Common;
using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;
using Elsa.Workflows.State;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class WorkflowCancellationServiceTests
{
    private static readonly DateTimeOffset MarkedAt = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Cancel_PersistsCancellingBeforeDispatch_ThenCommitsCancelled()
    {
        var store = CreateStore(Suspended("wf-1"));
        var clock = new FixedClock(MarkedAt);
        var dispatcher = new RecordingDispatcher(store, clock);
        var sut = CreateSut(store, dispatcher, clock);

        var cancelled = await sut.CancelWorkflowAsync("wf-1");

        Assert.True(cancelled);
        var request = Assert.Single(dispatcher.Requests);
        Assert.Equal(WorkflowSubStatus.Cancelling, dispatcher.StatusAtDispatch["wf-1"]);
        Assert.Equal(WorkflowSubStatus.Suspended, request.PreviousSubStatus);
        Assert.Equal(MarkedAt, request.CancellingAt);
        var stored = await store.FindAsync(new WorkflowInstanceFilter { Id = "wf-1" });
        Assert.NotNull(stored);
        Assert.Equal(WorkflowStatus.Finished, stored.Status);
        Assert.Equal(WorkflowSubStatus.Cancelled, stored.SubStatus);
    }

    [Fact]
    public async Task Cancel_DispatchFailure_RestoresPreviousSubStatus()
    {
        var state = SuspendedState();
        var store = CreateStore(Suspended("wf-1", state));
        var clock = new FixedClock(MarkedAt);
        var dispatcher = new RecordingDispatcher(store, clock) { Fail = true };
        var sut = CreateSut(store, dispatcher, clock);

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CancelWorkflowAsync("wf-1"));

        Assert.Equal("Dispatch failed.", failure.Message);
        Assert.Equal(WorkflowSubStatus.Cancelling, dispatcher.StatusAtDispatch["wf-1"]);
        var stored = await store.FindAsync(new WorkflowInstanceFilter { Id = "wf-1" });
        Assert.NotNull(stored);
        Assert.Equal(WorkflowStatus.Running, stored.Status);
        Assert.Equal(WorkflowSubStatus.Suspended, stored.SubStatus);
        Assert.Equal(MarkedAt.AddMinutes(5), stored.UpdatedAt);
        Assert.Equal(WorkflowSubStatus.Suspended, state.SubStatus);
        Assert.Equal("bm-1", Assert.Single(state.Bookmarks).Id);
    }

    [Fact]
    public async Task Cancel_DispatchFailureAfterCancelledCommit_DoesNotRestore()
    {
        var store = CreateStore(Suspended("wf-1"));
        var clock = new FixedClock(MarkedAt);
        var dispatcher = new RecordingDispatcher(store, clock) { CommitCancelledBeforeFailure = true };
        var sut = CreateSut(store, dispatcher, clock);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CancelWorkflowAsync("wf-1"));

        var stored = await store.FindAsync(new WorkflowInstanceFilter { Id = "wf-1" });
        Assert.NotNull(stored);
        Assert.Equal(WorkflowStatus.Finished, stored.Status);
        Assert.Equal(WorkflowSubStatus.Cancelled, stored.SubStatus);
    }

    [Fact]
    public async Task Cancel_ExecutingInstance_IsNotOverwrittenAndStillDispatched()
    {
        var store = CreateStore(new WorkflowInstance
        {
            Id = "wf-1",
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Version = 1,
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Executing,
            IsExecuting = true,
            UpdatedAt = MarkedAt.AddDays(-1)
        });
        var clock = new FixedClock(MarkedAt);
        var dispatcher = new RecordingDispatcher(store, clock);
        var sut = CreateSut(store, dispatcher, clock);

        Assert.True(await sut.CancelWorkflowAsync("wf-1"));

        var request = Assert.Single(dispatcher.Requests);
        Assert.Null(request.PreviousSubStatus);
        Assert.Null(request.CancellingAt);
        Assert.Equal(WorkflowSubStatus.Executing, dispatcher.StatusAtDispatch["wf-1"]);
        var stored = await store.FindAsync(new WorkflowInstanceFilter { Id = "wf-1" });
        Assert.NotNull(stored);
        Assert.Equal(WorkflowSubStatus.Executing, stored.SubStatus);
        Assert.True(stored.IsExecuting);
        Assert.Equal(MarkedAt.AddDays(-1), stored.UpdatedAt);
    }

    [Fact]
    public async Task Cancel_OwnedInstance_IsNotOverwrittenAndStillDispatched()
    {
        var store = CreateStore(Suspended("wf-1"));
        var clock = new FixedClock(MarkedAt);
        var dispatcher = new RecordingDispatcher(store, clock);
        var guard = Substitute.For<IWorkflowExecutionGuard>();
        guard.DemandUnownedAsync("wf-1", Arg.Any<CancellationToken>())
            .Returns<ValueTask>(_ => throw new InvalidOperationException("This workflow instance is owned by durable admission."));
        var sut = CreateSut(store, dispatcher, clock, guard);

        Assert.True(await sut.CancelWorkflowAsync("wf-1"));

        var request = Assert.Single(dispatcher.Requests);
        Assert.Null(request.PreviousSubStatus);
        var stored = await store.FindAsync(new WorkflowInstanceFilter { Id = "wf-1" });
        Assert.NotNull(stored);
        Assert.Equal(WorkflowSubStatus.Suspended, stored.SubStatus);
    }

    [Fact]
    public async Task Cancel_WhenConditionalMarkReturnsFalse_DoesNotSaveTheInstance()
    {
        var store = Substitute.For<IWorkflowInstanceStore>();
        store.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(Suspended("wf-1")));
        store.FindManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<IEnumerable<WorkflowInstance>>(Array.Empty<WorkflowInstance>()));
        store.TryMarkCancellingAsync(Arg.Any<string>(), Arg.Any<WorkflowSubStatus>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(false));
        var dispatcher = Substitute.For<IWorkflowCancellationDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<DispatchCancelWorkflowRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DispatchCancelWorkflowsResponse());
        var sut = CreateSut(store, dispatcher, new FixedClock(MarkedAt));

        Assert.True(await sut.CancelWorkflowAsync("wf-1"));

        await store.DidNotReceive().SaveAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().UpdateAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().SaveManyAsync(Arg.Any<IEnumerable<WorkflowInstance>>(), Arg.Any<CancellationToken>());
        await dispatcher.Received(1).DispatchAsync(
            Arg.Is<DispatchCancelWorkflowRequest>(request => request.WorkflowInstanceId == "wf-1" && request.PreviousSubStatus == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancel_StillCancelsChildInstances()
    {
        var store = CreateStore(Suspended("parent"), Suspended("child", parentId: "parent"));
        var clock = new FixedClock(MarkedAt);
        var dispatcher = new RecordingDispatcher(store, clock);
        var sut = CreateSut(store, dispatcher, clock);

        var count = await sut.CancelWorkflowAsync("parent");

        Assert.True(count);
        Assert.Equal(WorkflowSubStatus.Cancelling, dispatcher.StatusAtDispatch["parent"]);
        Assert.Equal(WorkflowSubStatus.Cancelling, dispatcher.StatusAtDispatch["child"]);
        Assert.Equal(2, dispatcher.Requests.Count);
    }

    [Fact]
    public async Task Cancel_FinishedInstance_IsLeftAlone()
    {
        var store = CreateStore(new WorkflowInstance
        {
            Id = "wf-1",
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Version = 1,
            Status = WorkflowStatus.Finished,
            SubStatus = WorkflowSubStatus.Finished
        });
        var dispatcher = new RecordingDispatcher(store, new FixedClock(MarkedAt));
        var sut = CreateSut(store, dispatcher, new FixedClock(MarkedAt));

        Assert.True(await sut.CancelWorkflowAsync("wf-1"));

        Assert.Empty(dispatcher.Requests);
        var stored = await store.FindAsync(new WorkflowInstanceFilter { Id = "wf-1" });
        Assert.NotNull(stored);
        Assert.Equal(WorkflowSubStatus.Finished, stored.SubStatus);
    }

    private static WorkflowCancellationService CreateSut(
        IWorkflowInstanceStore store,
        IWorkflowCancellationDispatcher dispatcher,
        ISystemClock clock,
        IWorkflowExecutionGuard? guard = null) =>
        new(
            Substitute.For<IWorkflowDefinitionService>(),
            store,
            dispatcher,
            clock,
            Substitute.For<ILogger<WorkflowCancellationService>>(),
            guard);

    private static MemoryWorkflowInstanceStore CreateStore(params WorkflowInstance[] instances)
    {
        var store = new MemoryWorkflowInstanceStore(new MemoryStore<WorkflowInstance>());
        foreach (var instance in instances)
            store.SaveAsync(instance).AsTask().GetAwaiter().GetResult();

        return store;
    }

    private static WorkflowState SuspendedState() => new()
    {
        Id = "wf-1",
        Status = WorkflowStatus.Running,
        SubStatus = WorkflowSubStatus.Suspended,
        Bookmarks = [new Bookmark("bm-1", "Wait", "hash", null, "activity", "node", "instance", default)]
    };

    private static WorkflowInstance Suspended(string id, WorkflowState? state = null, string? parentId = null) => new()
    {
        Id = id,
        DefinitionId = "def-1",
        DefinitionVersionId = "ver-1",
        Version = 1,
        ParentWorkflowInstanceId = parentId,
        Status = WorkflowStatus.Running,
        SubStatus = WorkflowSubStatus.Suspended,
        IsExecuting = false,
        UpdatedAt = MarkedAt.AddDays(-1),
        WorkflowState = state ?? SuspendedState()
    };

    private sealed class FixedClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }

    private sealed class RecordingDispatcher(MemoryWorkflowInstanceStore store, FixedClock clock) : IWorkflowCancellationDispatcher
    {
        public List<DispatchCancelWorkflowRequest> Requests { get; } = [];

        public Dictionary<string, WorkflowSubStatus> StatusAtDispatch { get; } = [];

        public bool Fail { get; init; }

        public bool CommitCancelledBeforeFailure { get; init; }

        public async Task<DispatchCancelWorkflowsResponse> DispatchAsync(DispatchCancelWorkflowRequest request, CancellationToken cancellationToken = default)
        {
            var instance = await store.FindAsync(new WorkflowInstanceFilter { Id = request.WorkflowInstanceId }, cancellationToken);
            if (instance is not null)
                StatusAtDispatch[request.WorkflowInstanceId] = instance.SubStatus;

            Requests.Add(request);

            if (CommitCancelledBeforeFailure && instance is not null)
            {
                instance.Status = WorkflowStatus.Finished;
                instance.SubStatus = WorkflowSubStatus.Cancelled;
                instance.UpdatedAt = MarkedAt.AddMinutes(1);
                await store.SaveAsync(instance, cancellationToken);
            }

            if (Fail || CommitCancelledBeforeFailure)
            {
                clock.UtcNow = MarkedAt.AddMinutes(5);
                throw new InvalidOperationException("Dispatch failed.");
            }

            if (instance is not null && request.PreviousSubStatus is not null)
            {
                instance.Status = WorkflowStatus.Finished;
                instance.SubStatus = WorkflowSubStatus.Cancelled;
                instance.UpdatedAt = MarkedAt.AddMinutes(1);
                await store.SaveAsync(instance, cancellationToken);
            }

            return new DispatchCancelWorkflowsResponse();
        }
    }
}
