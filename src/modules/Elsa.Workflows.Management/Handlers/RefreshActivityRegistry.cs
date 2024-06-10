using Elsa.Mediator.Contracts;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Notifications;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Handlers;

/// <summary>
/// Refreshes the <see cref="IActivityRegistry"/> for the <see cref="WorkflowDefinitionActivityProvider"/> provider whenever an <see cref="WorkflowDefinition"/> is published, retracted or deleted.
/// </summary>
[PublicAPI]
public class RefreshActivityRegistry(IWorkflowDefinitionActivityRegistryUpdater workflowDefinitionActivityRegistryUpdater) :
    INotificationHandler<WorkflowDefinitionPublished>,
    INotificationHandler<WorkflowDefinitionRetracted>,
    INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionsDeleted>,
    INotificationHandler<WorkflowDefinitionCreated>,
    INotificationHandler<WorkflowDefinitionVersionDeleted>,
    INotificationHandler<WorkflowDefinitionVersionsDeleted>,
    INotificationHandler<WorkflowDefinitionVersionsUpdated>
{
    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken)
    {
        return UpdateDefinition(notification.WorkflowDefinition.Id, notification.WorkflowDefinition.Options.UsableAsActivity);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken)
    { 
        workflowDefinitionActivityRegistryUpdater.RemoveDefinitionVersionFromRegistry(notification.WorkflowDefinition.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken)
    { 
        workflowDefinitionActivityRegistryUpdater.RemoveDefinitionFromRegistry(notification.DefinitionId);
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionsDeleted notification, CancellationToken cancellationToken)
    {
        foreach (string id in notification.DefinitionIds)
        {
            workflowDefinitionActivityRegistryUpdater.RemoveDefinitionFromRegistry(id);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionCreated notification, CancellationToken cancellationToken)
    {
        return UpdateDefinition(notification.WorkflowDefinition.Id, notification.WorkflowDefinition.Options.UsableAsActivity);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionVersionDeleted notification, CancellationToken cancellationToken)
    { 
        workflowDefinitionActivityRegistryUpdater.RemoveDefinitionVersionFromRegistry(notification.WorkflowDefinition.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionVersionsDeleted notification, CancellationToken cancellationToken)
    {
        foreach (string id in notification.Ids)
        {
            workflowDefinitionActivityRegistryUpdater.RemoveDefinitionVersionFromRegistry(id);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionVersionsUpdated notification, CancellationToken cancellationToken)
    {
        foreach (var definition in notification.VersionUpdates)
        {
            await UpdateDefinition(definition.Id, definition.UsableAsActivity);
        }
    }

    private Task UpdateDefinition(string id, bool? usableAsActivity)
    {
        if (usableAsActivity.GetValueOrDefault())
            return workflowDefinitionActivityRegistryUpdater.AddToRegistry(id);

        workflowDefinitionActivityRegistryUpdater.RemoveDefinitionVersionFromRegistry(id);
        return Task.CompletedTask;
    }
}