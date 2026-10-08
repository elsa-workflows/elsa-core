using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Notifications;
using JetBrains.Annotations;

namespace Elsa.Studio.Workflows.Handlers;

/// A handler that refreshes the activity registry when a workflow definition is deleted, published or retracted.
[UsedImplicitly]
/// <summary>
/// Represents the refresh activity registry.
/// </summary>
public class RefreshActivityRegistry : INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionPublished>,
    INotificationHandler<WorkflowDefinitionRetracted>,
    INotificationHandler<BulkWorkflowDefinitionsDeleted>,
    INotificationHandler<BulkWorkflowDefinitionVersionsDeleted>,
    INotificationHandler<BulkWorkflowDefinitionsPublished>,
    INotificationHandler<BulkWorkflowDefinitionsRetracted>,
    INotificationHandler<WorkflowDefinitionSaved>
{
    private readonly IActivityRegistry _activityRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshActivityRegistry"/> class.
    /// </summary>
    public RefreshActivityRegistry(IActivityRegistry activityRegistry)
    {
        _activityRegistry = activityRegistry;
    }

    async Task INotificationHandler<WorkflowDefinitionDeleted>.HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken) => await RefreshActivityRegistryAsync(cancellationToken);
    async Task INotificationHandler<WorkflowDefinitionSaved>.HandleAsync(WorkflowDefinitionSaved notification, CancellationToken cancellationToken) => await RefreshActivityRegistryAsync(cancellationToken);
    async Task INotificationHandler<WorkflowDefinitionPublished>.HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken) => await RefreshActivityRegistryAsync(cancellationToken);
    async Task INotificationHandler<WorkflowDefinitionRetracted>.HandleAsync(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken) => await RefreshActivityRegistryAsync(cancellationToken);
    async Task INotificationHandler<BulkWorkflowDefinitionsDeleted>.HandleAsync(BulkWorkflowDefinitionsDeleted notification, CancellationToken cancellationToken) => await RefreshActivityRegistryAsync(cancellationToken);
    async Task INotificationHandler<BulkWorkflowDefinitionVersionsDeleted>.HandleAsync(BulkWorkflowDefinitionVersionsDeleted notification, CancellationToken cancellationToken) => await RefreshActivityRegistryAsync(cancellationToken);
    async Task INotificationHandler<BulkWorkflowDefinitionsPublished>.HandleAsync(BulkWorkflowDefinitionsPublished notification, CancellationToken cancellationToken) => await RefreshActivityRegistryAsync(cancellationToken);
    async Task INotificationHandler<BulkWorkflowDefinitionsRetracted>.HandleAsync(BulkWorkflowDefinitionsRetracted notification, CancellationToken cancellationToken) => await RefreshActivityRegistryAsync(cancellationToken);

    private async Task RefreshActivityRegistryAsync(CancellationToken cancellationToken = default)
    {
        await _activityRegistry.RefreshAsync(cancellationToken);
    }
}