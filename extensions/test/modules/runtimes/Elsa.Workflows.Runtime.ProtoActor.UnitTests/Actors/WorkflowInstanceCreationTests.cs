using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;
using Elsa.Workflows.Runtime.ProtoActor.UnitTests.Fixtures;
using Elsa.Workflows.State;
using Moq;
using Proto.Cluster;

namespace Elsa.Workflows.Runtime.ProtoActor.UnitTests.Actors;

public class WorkflowInstanceCreationTests
{
    private const string InstanceId = "invoice-run-2026";
    private const string ActorId = "WorkflowInstanceActor-" + InstanceId;

    [Fact]
    public async Task CreateAndRunExecutesNewlyReservedInstance()
    {
        await using var harness = new WorkflowInstanceTestHarness(instanceId: InstanceId);
        var state = WorkflowInstanceTestHarness.CreateState(instanceId: InstanceId);
        harness.SetupNewInstance(state);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Assert.IsType<RunWorkflowInstanceResponse>(await harness.CreateAndRunAsync(timeout.Token));
        harness.WorkflowRunner.Verify(x => x.RunAsync(It.IsAny<WorkflowGraph>(), state,
            It.Is<RunWorkflowOptions>(options => options.WorkflowInstanceId == InstanceId), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreationReturnsConflictAfterActorHasCreatedInstance(bool createAndRun)
    {
        await using var harness = new WorkflowInstanceTestHarness(instanceId: InstanceId);
        var state = WorkflowInstanceTestHarness.CreateState(instanceId: InstanceId);
        harness.SetupNewInstance(state);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Assert.IsType<CreateWorkflowInstanceResponse>(await harness.CreateAsync(timeout.Token));

        var response = createAndRun
            ? await harness.CreateAndRunAsync(timeout.Token)
            : await harness.CreateAsync(timeout.Token);

        AssertConflict(response);
        harness.WorkflowInstanceManager.Verify(x => x.CreateAndCommitWorkflowInstanceAsync(
            It.IsAny<Workflow>(), It.IsAny<WorkflowInstanceOptions>(), It.IsAny<CancellationToken>()), Times.Once());
        harness.WorkflowRunner.Verify(x => x.RunAsync(It.IsAny<WorkflowGraph>(), It.IsAny<WorkflowState>(),
            It.IsAny<RunWorkflowOptions?>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task CreateAndRunReturnsConflictWhenStoreRejectsReservation()
    {
        await using var harness = new WorkflowInstanceTestHarness(instanceId: InstanceId);
        harness.WorkflowInstanceManager
            .Setup(x => x.CreateAndCommitWorkflowInstanceAsync(It.IsAny<Workflow>(), It.IsAny<WorkflowInstanceOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new WorkflowInstanceAlreadyExistsException(InstanceId));

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        AssertConflict(await harness.CreateAndRunAsync(timeout.Token));
        harness.WorkflowRunner.Verify(x => x.RunAsync(It.IsAny<WorkflowGraph>(), It.IsAny<WorkflowState>(),
            It.IsAny<RunWorkflowOptions?>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task ColdActorLoadsInstanceUsingFullIdentifier()
    {
        await using var harness = new WorkflowInstanceTestHarness(instanceId: InstanceId);
        var state = WorkflowInstanceTestHarness.CreateState(instanceId: InstanceId);
        harness.SetupExistingInstance(state);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Assert.IsType<RunWorkflowInstanceResponse>(await harness.RunAsync(timeout.Token));
        harness.WorkflowInstanceManager.Verify(x => x.FindByIdAsync(InstanceId, It.IsAny<CancellationToken>()), Times.Once());
        harness.WorkflowInstanceManager.Verify(x => x.FindByIdAsync(It.Is<string>(id => id != InstanceId),
            It.IsAny<CancellationToken>()), Times.Never());
        harness.WorkflowRunner.Verify(x => x.RunAsync(It.IsAny<WorkflowGraph>(), state,
            It.Is<RunWorkflowOptions>(options => options.WorkflowInstanceId == InstanceId), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task FailedCreationDoesNotChangeInstanceExistence()
    {
        await using var harness = new WorkflowInstanceTestHarness(instanceId: InstanceId);
        harness.WorkflowInstanceManager
            .Setup(x => x.CreateAndCommitWorkflowInstanceAsync(It.IsAny<Workflow>(), It.IsAny<WorkflowInstanceOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Reservation failed."));

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Assert.IsType<GrainErrorResponse>(await harness.CreateAsync(timeout.Token));
        var response = Assert.IsType<InstanceExistsResponse>(await harness.InstanceExistsAsync(timeout.Token));
        Assert.False(response.Exists);
        harness.WorkflowInstanceManager.Verify(x => x.ExistsAsync(InstanceId, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task ColdActorReportsExistenceUsingFullIdentifier()
    {
        await using var harness = new WorkflowInstanceTestHarness(instanceId: InstanceId);
        harness.WorkflowInstanceManager.Setup(x => x.ExistsAsync(InstanceId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var response = Assert.IsType<InstanceExistsResponse>(await harness.InstanceExistsAsync(timeout.Token));
        Assert.True(response.Exists);
    }

    private static void AssertConflict(object response)
    {
        var error = Assert.IsType<GrainErrorResponse>(response);
        var conflict = Assert.IsType<WorkflowInstanceAlreadyExistsException>(WorkflowInstanceRpcErrors.ToException(ActorId, error.Err));
        Assert.Equal(InstanceId, conflict.WorkflowInstanceId);
    }
}
