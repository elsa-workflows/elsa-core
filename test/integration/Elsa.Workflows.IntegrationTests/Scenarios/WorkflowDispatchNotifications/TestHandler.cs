using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowDispatchNotifications;

public class TestHandler : 
    INotificationHandler<WorkflowDefinitionDispatching>, 
    INotificationHandler<WorkflowDefinitionDispatched>,
    INotificationHandler<WorkflowInstanceDispatching>,
    INotificationHandler<WorkflowInstanceDispatched>
{
    private readonly Spy _spy;

    public TestHandler(Spy spy)
    {
        _spy = spy;
    }
    
    public Task HandleAsync(WorkflowDefinitionDispatching notification, CancellationToken cancellationToken)
    {
        _spy.WorkflowDefinitionDispatchingWasCalled = true;
        _spy.CapturedDefinitionRequest = notification.Request;
        return Task.CompletedTask;
    }

    public Task HandleAsync(WorkflowDefinitionDispatched notification, CancellationToken cancellationToken)
    {
        _spy.WorkflowDefinitionDispatchedWasCalled = true;
        _spy.CapturedResponse = notification.Response;
        return Task.CompletedTask;
    }

    public Task HandleAsync(WorkflowInstanceDispatching notification, CancellationToken cancellationToken)
    {
        _spy.WorkflowInstanceDispatchingWasCalled = true;
        _spy.CapturedInstanceRequest = notification.Request;
        return Task.CompletedTask;
    }

    public Task HandleAsync(WorkflowInstanceDispatched notification, CancellationToken cancellationToken)
    {
        _spy.WorkflowInstanceDispatchedWasCalled = true;
        _spy.CapturedResponse = notification.Response;
        return Task.CompletedTask;
    }
}
