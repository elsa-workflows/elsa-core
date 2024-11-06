using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Notifications;

namespace Elsa.Workflows.Management.Handlers;

public class UpdateConsumingWorkflows(IWorkflowGraphNetworkBuilder workflowGraphNetworkBuilder) : INotificationHandler<WorkflowDefinitionPublished>
{
    public async Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken)
    {
        var network = await workflowGraphNetworkBuilder.BuildAsync(cancellationToken);
    }
}