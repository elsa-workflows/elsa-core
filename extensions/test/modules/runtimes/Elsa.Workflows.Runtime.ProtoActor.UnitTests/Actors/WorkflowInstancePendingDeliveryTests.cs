using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;
using Elsa.Workflows.Runtime.ProtoActor.UnitTests.Fixtures;
using Elsa.Workflows.State;
using Moq;
using Proto.Cluster;
using WorkflowInstanceEntity = Elsa.Workflows.Management.Entities.WorkflowInstance;

namespace Elsa.Workflows.Runtime.ProtoActor.UnitTests.Actors;

public class WorkflowInstancePendingDeliveryTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("different", false)]
    [InlineData("owner", true)]
    public async Task RunsPendingStateOnlyForItsRecordedDelivery(string? deliveryId, bool starts)
    {
        await using var harness = new WorkflowInstanceTestHarness();
        var state = WorkflowInstanceTestHarness.CreateState();
        state.SubStatus = WorkflowSubStatus.Pending;
        state.Properties[WorkflowStartDelivery.IdProperty] = "owner";
        harness.SetupExistingInstance(state);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var result = Assert.IsType<RunIfPendingWorkflowInstanceResponse>(
            await harness.RunIfPendingAsync(deliveryId, timeout.Token));

        Assert.Equal(starts, result.Execution != null);
        harness.WorkflowRunner.Verify(x => x.RunAsync(It.IsAny<WorkflowGraph>(), state,
            It.IsAny<RunWorkflowOptions?>(), It.IsAny<CancellationToken>()), starts ? Times.Once() : Times.Never());
    }

    [Fact]
    public async Task PendingStartKeepsActorAvailableForCancellation()
    {
        await using var harness = new WorkflowInstanceTestHarness();
        var state = WorkflowInstanceTestHarness.CreateState();
        state.SubStatus = WorkflowSubStatus.Pending;
        state.Properties[WorkflowStartDelivery.IdProperty] = "owner";
        harness.SetupExistingInstance(state);
        harness.WorkflowCanceler
            .Setup(x => x.CancelWorkflowAsync(It.IsAny<WorkflowGraph>(), state, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                state.SubStatus = WorkflowSubStatus.Cancelled;
                return state;
            });

        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.WorkflowRunner
            .Setup(x => x.RunAsync(It.IsAny<WorkflowGraph>(), state, It.IsAny<RunWorkflowOptions?>(), It.IsAny<CancellationToken>()))
            .Returns(async (WorkflowGraph graph, WorkflowState workflowState, RunWorkflowOptions? _, CancellationToken token) =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(token);
                return new RunWorkflowResult(null!, workflowState, graph.Workflow, null, Journal.Empty);
            });

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var run = harness.RunIfPendingAsync("owner", timeout.Token);
        try
        {
            await started.Task.WaitAsync(timeout.Token);
            var duplicate = harness.RunIfPendingAsync("owner", timeout.Token);
            Assert.IsType<GrainResponseMessage>(await harness.CancelAsync(timeout.Token));
            Assert.IsType<GrainErrorResponse>(await run);
            Assert.Null(Assert.IsType<RunIfPendingWorkflowInstanceResponse>(await duplicate).Execution);
            harness.WorkflowCanceler.Verify(x => x.CancelWorkflowAsync(It.IsAny<WorkflowGraph>(), state,
                It.IsAny<CancellationToken>()), Times.Once());
        }
        finally
        {
            release.TrySetResult();
        }
    }

    [Fact]
    public async Task ChecksEachPendingDeliveryAfterLoadingColdActorState()
    {
        await using var harness = new WorkflowInstanceTestHarness();
        var state = WorkflowInstanceTestHarness.CreateState();
        state.SubStatus = WorkflowSubStatus.Pending;
        state.Properties[WorkflowStartDelivery.IdProperty] = "owner";
        var lookupStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var storedInstance = new TaskCompletionSource<WorkflowInstanceEntity?>(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.WorkflowInstanceManager
            .Setup(x => x.FindByIdAsync(state.Id, It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                lookupStarted.TrySetResult();
                return storedInstance.Task;
            });

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var first = harness.RunIfPendingAsync("earlier-delivery", timeout.Token);
        try
        {
            await lookupStarted.Task.WaitAsync(timeout.Token);
            var owner = harness.RunIfPendingAsync("owner", timeout.Token);
            Assert.IsType<InstanceExistsResponse>(await harness.InstanceExistsAsync(timeout.Token));
            storedInstance.SetResult(WorkflowInstanceTestHarness.CreateWorkflowInstance(state));

            Assert.Null(Assert.IsType<RunIfPendingWorkflowInstanceResponse>(await first).Execution);
            Assert.NotNull(Assert.IsType<RunIfPendingWorkflowInstanceResponse>(await owner).Execution);
            harness.WorkflowRunner.Verify(x => x.RunAsync(It.IsAny<WorkflowGraph>(), state,
                It.IsAny<RunWorkflowOptions?>(), It.IsAny<CancellationToken>()), Times.Once());
        }
        finally
        {
            storedInstance.TrySetResult(null);
        }
    }
}
