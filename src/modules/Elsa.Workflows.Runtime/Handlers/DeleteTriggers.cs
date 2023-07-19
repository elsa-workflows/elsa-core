using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Deletes bookmarks for workflow instances being deleted.
/// </summary>
public class DeleteTriggers : 
    INotificationHandler<WorkflowDefinitionDeleting>,
    INotificationHandler<WorkflowDefinitionVersionDeleting>,
    INotificationHandler<WorkflowDefinitionsDeleting>,
    INotificationHandler<WorkflowDefinitionVersionsDeleting>
{
    private readonly ITriggerStore _triggerStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteBookmarks"/> class.
    /// </summary>
    public DeleteTriggers(ITriggerStore triggerStore)
    {
        _triggerStore = triggerStore;
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionDeleting notification, CancellationToken cancellationToken)
    {
        await DeleteTriggersAsync(new TriggerFilter { WorkflowDefinitionId = notification.DefinitionId }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionVersionDeleting notification, CancellationToken cancellationToken)
    {
        await DeleteTriggersAsync(new TriggerFilter { WorkflowDefinitionVersionId = notification.WorkflowDefinition.Id }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionsDeleting notification, CancellationToken cancellationToken)
    {
        await DeleteTriggersAsync(new TriggerFilter { WorkflowDefinitionIds = notification.DefinitionIds }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionVersionsDeleting notification, CancellationToken cancellationToken)
    {
        await DeleteTriggersAsync(new TriggerFilter { WorkflowDefinitionVersionIds = notification.Ids }, cancellationToken);
    }

    private async Task DeleteTriggersAsync(TriggerFilter filter, CancellationToken cancellationToken) => await _triggerStore.DeleteManyAsync(filter, cancellationToken);
}