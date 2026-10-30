using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Notifications;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Handlers;

public class DispatchWorkflowCommandHandlerTests
{
    private readonly IStimulusSender _stimulusSender = Substitute.For<IStimulusSender>();
    private readonly IWorkflowRuntime _workflowRuntime = Substitute.For<IWorkflowRuntime>();
    private readonly IWorkflowClient _workflowClient = Substitute.For<IWorkflowClient>();
    private readonly INotificationSender _notificationSender = Substitute.For<INotificationSender>();
    private readonly DispatchWorkflowCommandHandler _handler;

    public DispatchWorkflowCommandHandlerTests()
    {
        _handler = new(_stimulusSender, _workflowRuntime, _notificationSender);
        _workflowRuntime.CreateClientAsync("child-1", Arg.Any<CancellationToken>()).Returns(new ValueTask<IWorkflowClient>(_workflowClient));
        _workflowClient.CreateAndRunInstanceAsync(Arg.Any<CreateAndRunWorkflowInstanceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new RunWorkflowInstanceResponse());
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

    [Fact]
    public async Task HandleAsync_ReportsActivationDenialForWaitingParent_WithoutChangingQueuedDispatchResult()
    {
        var properties = new Dictionary<string, object> { ["WaitForCompletion"] = true };
        DispatchWorkflowActivationDeniedRoute.Add(properties, "Elsa.DispatchWorkflow", "dispatch-hash");
        var command = new DispatchWorkflowDefinitionCommand("definition-version-1")
        {
            InstanceId = "child-1",
            ParentWorkflowInstanceId = "parent-1",
            Properties = properties
        };
        _workflowClient.CreateAndRunInstanceAsync(Arg.Any<CreateAndRunWorkflowInstanceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new RunWorkflowInstanceResponse { CannotStart = true });

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Same(Unit.Instance, result);
        await _notificationSender.Received(1).SendAsync(
            Arg.Is<DispatchWorkflowActivationDenied>(notification => notification.ParentWorkflowInstanceId == "parent-1" && notification.WorkflowInstanceId == "child-1" && notification.ActivityTypeName == "Elsa.DispatchWorkflow" && notification.StimulusHash == "dispatch-hash"),
            Arg.Any<CancellationToken>());

        Assert.Equal("Elsa.DispatchWorkflow", properties["__elsa.internal.dispatch-activation-denied.activity-type-name"]);
        Assert.Equal("dispatch-hash", properties["__elsa.internal.dispatch-activation-denied.stimulus-hash"]);
    }

    [Fact]
    public async Task HandleAsync_DoesNotReportActivationDenialForFireAndForgetDispatch()
    {
        var command = new DispatchWorkflowDefinitionCommand("definition-version-1")
        {
            InstanceId = "child-1",
            ParentWorkflowInstanceId = "parent-1",
            Properties = new Dictionary<string, object> { ["WaitForCompletion"] = false }
        };
        _workflowClient.CreateAndRunInstanceAsync(Arg.Any<CreateAndRunWorkflowInstanceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new RunWorkflowInstanceResponse { CannotStart = true });

        await _handler.HandleAsync(command, CancellationToken.None);

        await _notificationSender.DidNotReceive().SendAsync(Arg.Any<DispatchWorkflowActivationDenied>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SanitizesActivationDenialRouteBeforePersistingProperties()
    {
        var properties = new Dictionary<string, object> { ["WaitForCompletion"] = true };
        DispatchWorkflowActivationDeniedRoute.Add(properties, "Elsa.DispatchWorkflow", "dispatch-hash");
        var command = new DispatchWorkflowDefinitionCommand("definition-version-1")
        {
            InstanceId = "child-1",
            ParentWorkflowInstanceId = "parent-1",
            Properties = properties
        };
        CreateAndRunWorkflowInstanceRequest? observedRequest = null;
        _workflowClient.CreateAndRunInstanceAsync(
                Arg.Do<CreateAndRunWorkflowInstanceRequest>(request => observedRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new RunWorkflowInstanceResponse());

        await _handler.HandleAsync(command, CancellationToken.None);

        Assert.NotNull(observedRequest);
        Assert.NotSame(properties, observedRequest.Properties);
        Assert.Equal(new Dictionary<string, object> { ["WaitForCompletion"] = true }, observedRequest.Properties);
        Assert.Contains("__elsa.internal.dispatch-activation-denied.activity-type-name", properties);
        Assert.Contains("__elsa.internal.dispatch-activation-denied.stimulus-hash", properties);
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
