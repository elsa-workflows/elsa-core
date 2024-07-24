using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class WorkflowDefinitionsReloader(IWorkflowDefinitionStorePopulator workflowDefinitionStorePopulator, INotificationSender notificationSender) : IWorkflowDefinitionsReloader
{
    /// <inheritdoc />
    public async Task ReloadWorkflowDefinitionsAsync(CancellationToken cancellationToken)
    {
        var workflowDefinitions = await workflowDefinitionStorePopulator.PopulateStoreAsync(true, cancellationToken);
        var reloadedWorkflowDefinitions = workflowDefinitions.Select(ReloadedWorkflowDefinition.FromDefinition).ToList();
        var notification = new WorkflowDefinitionsReloaded(reloadedWorkflowDefinitions);
        await notificationSender.SendAsync(notification, cancellationToken);
    }
}