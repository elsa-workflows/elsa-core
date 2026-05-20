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
    public async Task HandleAsync_CreatesAndRunsWorkflow_WhenCommandInstanceAlreadyExistsAndIdempotencyIsNotRequested()
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
