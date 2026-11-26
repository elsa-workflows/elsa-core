using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Deletes the triggers of workflow definitions being deleted. After a tenant deleted versions of its own definition, it may have stopped
/// overriding a tenant-agnostic definition with the same ID, so the tenant's scheduled triggers of that definition are reconciled again.
/// </summary>
[UsedImplicitly]
public class DeleteTriggers(ITriggerIndexer triggerIndexer, ILogger<DeleteTriggers>? logger) :
    INotificationHandler<WorkflowDefinitionDeleting>,
    INotificationHandler<WorkflowDefinitionVersionDeleting>,
    INotificationHandler<WorkflowDefinitionsDeleting>,
    INotificationHandler<WorkflowDefinitionVersionsDeleting>,
    INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionsDeleted>,
    INotificationHandler<WorkflowDefinitionVersionDeleted>,
    INotificationHandler<WorkflowDefinitionVersionsDeleted>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteTriggers"/> class.
    /// </summary>
    [Obsolete("Use the constructor that accepts an ILogger<DeleteTriggers> instead. Removed in the next major version.")]
    public DeleteTriggers(ITriggerIndexer triggerIndexer) : this(triggerIndexer, null)
    {
    }

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
            WorkflowDefinitionVersionIds = notification.Ids,
            RequireCompleteCleanup = notification.RequireCompleteTriggerCleanup
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken) => ReconcileTenantCopiesAsync([notification.DefinitionId], true, cancellationToken);

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionsDeleted notification, CancellationToken cancellationToken) => ReconcileTenantCopiesAsync(notification.DefinitionIds, true, cancellationToken);

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionVersionDeleted notification, CancellationToken cancellationToken) => ReconcileTenantCopiesAsync([notification.WorkflowDefinition.DefinitionId], false, cancellationToken);

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionVersionsDeleted notification, CancellationToken cancellationToken) => ReconcileTenantCopiesAsync(notification.DefinitionIds, false, cancellationToken);

    /// <param name="definitionIds">The IDs of the definitions whose versions were deleted.</param>
    /// <param name="byDefinitionId">
    /// Whether whole definitions were deleted. That unschedules the tenant's copies of a tenant-agnostic definition with the same ID too,
    /// so they are scheduled again; deleting single versions leaves unchanged copies as they are.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private Task ReconcileTenantCopiesAsync(ICollection<string> definitionIds, bool byDefinitionId, CancellationToken cancellationToken) =>
        definitionIds.Count > 0 && TriggerIndexer.AsTriggerIndexer(triggerIndexer, logger) is { } indexer
            ? indexer.ReconcileTenantCopiesAfterDeletionAsync(definitionIds, byDefinitionId, cancellationToken)
            : Task.CompletedTask;

    private async Task DeleteTriggersAsync(TriggerFilter filter, CancellationToken cancellationToken)
    {
        await triggerIndexer.DeleteTriggersAsync(filter, cancellationToken);
    }
}