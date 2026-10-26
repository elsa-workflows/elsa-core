using Elsa.Common;
using Elsa.Common.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.Requests;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Handlers;

public class CancelWorkflowsCommandHandlerTests
{
    private static readonly DateTimeOffset CancellingAt = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_TerminalFailure_RestoresPreviousSubStatus()
    {
        var store = await StoreMarkedCancelling();
        var clock = new FixedClock(CancellingAt.AddMinutes(3));
        var client = Substitute.For<IWorkflowClient>();
        client.CancelAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new InvalidOperationException("This workflow instance is owned by durable admission.")));
        var sut = CreateSut(store, client, clock);

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.HandleAsync(Command(), CancellationToken.None));

        Assert.Equal("This workflow instance is owned by durable admission.", failure.Message);
        var stored = await store.FindAsync(new WorkflowInstanceFilter { Id = "wf-1" });
        Assert.NotNull(stored);
        Assert.Equal(WorkflowStatus.Running, stored.Status);
        Assert.Equal(WorkflowSubStatus.Suspended, stored.SubStatus);
        Assert.Equal(CancellingAt.AddMinutes(3), stored.UpdatedAt);
    }

    [Fact]
    public async Task Handle_SuccessfulCancel_LeavesTheCommittedCancelledState()
    {
        var store = await StoreMarkedCancelling();
        var client = Substitute.For<IWorkflowClient>();
        client.CancelAsync(Arg.Any<CancellationToken>())
            .Returns(_ => CommitCancelled(store));
        var sut = CreateSut(store, client, new FixedClock(CancellingAt.AddMinutes(3)));

        await sut.HandleAsync(Command(), CancellationToken.None);

        var stored = await store.FindAsync(new WorkflowInstanceFilter { Id = "wf-1" });
        Assert.NotNull(stored);
        Assert.Equal(WorkflowStatus.Finished, stored.Status);
        Assert.Equal(WorkflowSubStatus.Cancelled, stored.SubStatus);
    }

    [Fact]
    public async Task Handle_FailureAfterCancelledCommit_DoesNotRestore()
    {
        var store = await StoreMarkedCancelling();
        var client = Substitute.For<IWorkflowClient>();
        client.CancelAsync(Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await CommitCancelled(store);
                throw new InvalidOperationException("after commit");
            });
        var sut = CreateSut(store, client, new FixedClock(CancellingAt.AddMinutes(3)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.HandleAsync(Command(), CancellationToken.None));

        var stored = await store.FindAsync(new WorkflowInstanceFilter { Id = "wf-1" });
        Assert.NotNull(stored);
        Assert.Equal(WorkflowSubStatus.Cancelled, stored.SubStatus);
        Assert.Equal(WorkflowStatus.Finished, stored.Status);
    }

    private static CancelWorkflowsCommandHandler CreateSut(MemoryWorkflowInstanceStore store, IWorkflowClient client, ISystemClock clock)
    {
        var runtime = Substitute.For<IWorkflowRuntime>();
        runtime.CreateClientAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(client);
        return new(runtime, store, clock, Substitute.For<ILogger<CancelWorkflowsCommandHandler>>());
    }

    private static async Task<MemoryWorkflowInstanceStore> StoreMarkedCancelling()
    {
        var store = new MemoryWorkflowInstanceStore(new MemoryStore<WorkflowInstance>());
        await store.SaveAsync(new WorkflowInstance
        {
            Id = "wf-1",
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Version = 1,
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Suspended,
            IsExecuting = false
        });
        Assert.True(await store.TryMarkCancellingAsync("wf-1", WorkflowSubStatus.Suspended, CancellingAt));
        return store;
    }

    private static Task CommitCancelled(MemoryWorkflowInstanceStore store) =>
        store.SaveAsync(new WorkflowInstance
        {
            Id = "wf-1",
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Version = 1,
            Status = WorkflowStatus.Finished,
            SubStatus = WorkflowSubStatus.Cancelled,
            IsExecuting = false,
            UpdatedAt = CancellingAt.AddMinutes(1)
        }).AsTask();

    private static CancelWorkflowsCommand Command() => new(new DispatchCancelWorkflowRequest
    {
        WorkflowInstanceId = "wf-1",
        PreviousSubStatus = WorkflowSubStatus.Suspended,
        CancellingAt = CancellingAt
    });

    private sealed class FixedClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
