using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.Messages;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Handlers;

public class DispatchWorkflowCommandHandlerTests
{
    private readonly IStimulusSender _stimulusSender = Substitute.For<IStimulusSender>();
    private readonly IWorkflowRuntime _workflowRuntime = Substitute.For<IWorkflowRuntime>();
    private readonly IWorkflowClient _workflowClient = Substitute.For<IWorkflowClient>();
    private readonly DispatchWorkflowCommandHandler _handler;

    public DispatchWorkflowCommandHandlerTests()
    {
        _handler = new(_stimulusSender, _workflowRuntime);
        _workflowRuntime.CreateClientAsync("child-1", Arg.Any<CancellationToken>()).Returns(new ValueTask<IWorkflowClient>(_workflowClient));
    }

    [Fact]
    public async Task HandleAsync_DoesNotCheckExistingInstanceAndCreatesAndRunsWorkflow_WhenIdempotencyIsNotRequested()
    {
        var command = new DispatchWorkflowDefinitionCommand("definition-version-1")
        {
            InstanceId = "child-1",
            SkipIfInstanceExists = false
        };
        _workflowClient.InstanceExistsAsync(Arg.Any<CancellationToken>()).Returns(true);

        await _handler.HandleAsync(command, CancellationToken.None);

        await _workflowClient.DidNotReceive().InstanceExistsAsync(Arg.Any<CancellationToken>());
        await _workflowClient.Received(1).CreateAndRunInstanceAsync(
            Arg.Is<CreateAndRunWorkflowInstanceRequest>(x => x.WorkflowDefinitionHandle.DefinitionVersionId == "definition-version-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PreservesDefinitionDispatchMetadata()
    {
        var input = new Dictionary<string, object> { ["input-key"] = "input-value" };
        var properties = new Dictionary<string, object> { ["property-key"] = "property-value" };
        var command = new DispatchWorkflowDefinitionCommand("definition-version-1")
        {
            InstanceId = "child-1",
            ParentWorkflowInstanceId = "parent-1",
            CorrelationId = "correlation-1",
            Input = input,
            Properties = properties,
            TriggerActivityId = "trigger-1",
            SchedulingActivityExecutionId = "scheduled-execution-1",
            SchedulingWorkflowInstanceId = "scheduling-parent-1",
            SchedulingCallStackDepth = 4
        };
        CreateAndRunWorkflowInstanceRequest? observedRequest = null;
        _workflowClient.CreateAndRunInstanceAsync(
                Arg.Do<CreateAndRunWorkflowInstanceRequest>(request => observedRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RunWorkflowInstanceResponse()));

        await _handler.HandleAsync(command, CancellationToken.None);

        Assert.NotNull(observedRequest);
        Assert.Equal("definition-version-1", observedRequest.WorkflowDefinitionHandle.DefinitionVersionId);
        Assert.Equal("correlation-1", observedRequest.CorrelationId);
        Assert.Same(input, observedRequest.Input);
        Assert.Same(properties, observedRequest.Properties);
        Assert.Equal("parent-1", observedRequest.ParentId);
        Assert.Equal("trigger-1", observedRequest.TriggerActivityId);
        Assert.Equal("scheduled-execution-1", observedRequest.SchedulingActivityExecutionId);
        Assert.Equal("scheduling-parent-1", observedRequest.SchedulingWorkflowInstanceId);
        Assert.Equal(4, observedRequest.SchedulingCallStackDepth);
    }

    [Fact]
    public async Task HandleAsync_DoesNotCreateAndRunWorkflow_WhenCommandInstanceAlreadyExistsAndIdempotencyIsRequested()
    {
        var command = new DispatchWorkflowDefinitionCommand("definition-version-1")
        {
            InstanceId = "child-1",
            SkipIfInstanceExists = true
        };
        _workflowClient.InstanceExistsAsync(Arg.Any<CancellationToken>()).Returns(true);

        await _handler.HandleAsync(command, CancellationToken.None);

        await _workflowClient.Received(1).InstanceExistsAsync(Arg.Any<CancellationToken>());
        await _workflowClient.DidNotReceiveWithAnyArgs().CreateAndRunInstanceAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_CreatesAndRunsWorkflow_WhenIdempotencyIsRequestedAndCommandInstanceDoesNotExist()
    {
        var command = new DispatchWorkflowDefinitionCommand("definition-version-1")
        {
            InstanceId = "child-1",
            SkipIfInstanceExists = true
        };
        _workflowClient.InstanceExistsAsync(Arg.Any<CancellationToken>()).Returns(false);

        await _handler.HandleAsync(command, CancellationToken.None);

        await _workflowClient.Received(1).InstanceExistsAsync(Arg.Any<CancellationToken>());
        await _workflowClient.Received(1).CreateAndRunInstanceAsync(
            Arg.Is<CreateAndRunWorkflowInstanceRequest>(x => x.WorkflowDefinitionHandle.DefinitionVersionId == "definition-version-1"),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task HandleAsync_DoesNotCheckExistingInstance_WhenCommandInstanceIdIsMissing(string? instanceId)
    {
        var command = new DispatchWorkflowDefinitionCommand("definition-version-1")
        {
            InstanceId = instanceId,
            SkipIfInstanceExists = true
        };
        _workflowRuntime.CreateClientAsync(instanceId, Arg.Any<CancellationToken>()).Returns(new ValueTask<IWorkflowClient>(_workflowClient));

        await _handler.HandleAsync(command, CancellationToken.None);

        await _workflowClient.DidNotReceive().InstanceExistsAsync(Arg.Any<CancellationToken>());
        await _workflowClient.Received(1).CreateAndRunInstanceAsync(
            Arg.Is<CreateAndRunWorkflowInstanceRequest>(x => x.WorkflowDefinitionHandle.DefinitionVersionId == "definition-version-1"),
            Arg.Any<CancellationToken>());
    }
}
