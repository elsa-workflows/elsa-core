using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class WorkflowDefinitionsReloader(IWorkflowDefinitionStorePopulator workflowDefinitionStorePopulator, INotificationSender notificationSender) : IWorkflowDefinitionsReloader
{
    /// <inheritdoc />
    public async Task ReloadWorkflowDefinitionsAsync(CancellationToken cancellationToken)
    {
        var definitionIds = await workflowDefinitionStorePopulator.PopulateStoreAsync(true, cancellationToken);
        var notification = new WorkflowDefinitionsReloaded(definitionIds);
        await notificationSender.SendAsync(notification, cancellationToken);
    }
}