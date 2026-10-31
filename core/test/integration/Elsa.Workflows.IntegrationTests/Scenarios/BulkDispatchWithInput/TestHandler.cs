using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BulkDispatchWithInput;

public class TestHandler(Spy spy) : INotificationHandler<WorkflowDefinitionDispatching>
{
    public Task HandleAsync(WorkflowDefinitionDispatching notification, CancellationToken cancellationToken)
    {
        spy.CaptureDispatch(notification.Request);
        return Task.CompletedTask;
    }
}
