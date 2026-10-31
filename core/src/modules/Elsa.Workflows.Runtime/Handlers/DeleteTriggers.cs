using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Deletes bookmarks for workflow definitions being deleted.
/// </summary>
[UsedImplicitly]
public class DeleteTriggers(ITriggerIndexer triggerIndexer) :
    INotificationHandler<WorkflowDefinitionDeleting>,
    INotificationHandler<WorkflowDefinitionVersionDeleting>,
    INotificationHandler<WorkflowDefinitionsDeleting>,
    INotificationHandler<WorkflowDefinitionVersionsDeleting>
{
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionDeleting notification, CancellationToken cancellationToken)
    {
        await DeleteTriggersAsync(new TriggerFilter
        {
            WorkflowDefinitionId = notification.DefinitionId
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionVersionDeleting notification, CancellationToken cancellationToken)
    {
        await DeleteTriggersAsync(new TriggerFilter
        {
            WorkflowDefinitionVersionId = notification.WorkflowDefinition.Id
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionsDeleting notification, CancellationToken cancellationToken)
    {
        await DeleteTriggersAsync(new TriggerFilter
        {
            WorkflowDefinitionIds = notification.DefinitionIds
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionVersionsDeleting notification, CancellationToken cancellationToken)
    {
        await DeleteTriggersAsync(new TriggerFilter
        {
            WorkflowDefinitionVersionIds = notification.Ids
        }, cancellationToken);
    }

    private async Task DeleteTriggersAsync(TriggerFilter filter, CancellationToken cancellationToken)
    {
        await triggerIndexer.DeleteTriggersAsync(filter, cancellationToken);
    }
}