using Elsa.Common.Multitenancy;
using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Requests;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class BackgroundWorkflowDispatcherTests
{
    private readonly ICommandSender _commandSender = Substitute.For<ICommandSender>();
    private readonly INotificationSender _notificationSender = Substitute.For<INotificationSender>();
    private readonly ITenantAccessor _tenantAccessor = Substitute.For<ITenantAccessor>();

    [Fact]
    public async Task DispatchWorkflowDefinition_DoesNotPropagateCallerToken()
    {
        // Arrange
        var dispatcher = CreateDispatcher();
        var request = new DispatchWorkflowDefinitionRequest("test-definition-id");
        using var callerCts = new CancellationTokenSource();

        // Act
        await dispatcher.DispatchAsync(request, cancellationToken: callerCts.Token);

        // Assert
        await _commandSender.Received(1).SendAsync(
            Arg.Any<DispatchWorkflowDefinitionCommand>(),
            CommandStrategy.Background,
            Arg.Any<IDictionary<object, object>>(),
            CancellationToken.None);
    }

    [Fact]
    public async Task DispatchWorkflowInstance_DoesNotPropagateCallerToken()
    {
        // Arrange
        var dispatcher = CreateDispatcher();
        var request = new DispatchWorkflowInstanceRequest("test-instance-id");
        using var callerCts = new CancellationTokenSource();

        // Act
        await dispatcher.DispatchAsync(request, cancellationToken: callerCts.Token);

        // Assert
        await _commandSender.Received(1).SendAsync(
            Arg.Any<DispatchWorkflowInstanceCommand>(),
            CommandStrategy.Background,
            Arg.Any<IDictionary<object, object>>(),
            CancellationToken.None);
    }

    [Fact]
    public async Task DispatchTriggerWorkflows_DoesNotPropagateCallerToken()
    {
        // Arrange
        var dispatcher = CreateDispatcher();
        var request = new DispatchTriggerWorkflowsRequest("TestActivityType", new { });
        using var callerCts = new CancellationTokenSource();

        // Act
        await dispatcher.DispatchAsync(request, cancellationToken: callerCts.Token);

        // Assert
        await _commandSender.Received(1).SendAsync(
            Arg.Any<DispatchTriggerWorkflowsCommand>(),
            CommandStrategy.Background,
            Arg.Any<IDictionary<object, object>>(),
            CancellationToken.None);
    }

    [Fact]
    public async Task DispatchResumeWorkflows_DoesNotPropagateCallerToken()
    {
        // Arrange
        var dispatcher = CreateDispatcher();
        var request = new DispatchResumeWorkflowsRequest("TestActivityType", new { });
        using var callerCts = new CancellationTokenSource();

        // Act
        await dispatcher.DispatchAsync(request, cancellationToken: callerCts.Token);

        // Assert
        await _commandSender.Received(1).SendAsync(
            Arg.Any<DispatchResumeWorkflowsCommand>(),
            CommandStrategy.Background,
            Arg.Any<IDictionary<object, object>>(),
            CancellationToken.None);
    }

    private BackgroundWorkflowDispatcher CreateDispatcher()
    {
        return new(_commandSender, _notificationSender, _tenantAccessor);
    }
}
