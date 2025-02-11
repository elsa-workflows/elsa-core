using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Notifications;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Handlers.Notification;

/// <summary>
/// Refreshes the <see cref="IActivityRegistry"/> for the <see cref="WorkflowDefinitionActivityProvider"/> provider whenever an <see cref="WorkflowDefinition"/> is published, retracted or deleted.
/// </summary>
[PublicAPI]
public class RefreshActivityRegistry(IWorkflowDefinitionActivityRegistryUpdater workflowDefinitionActivityRegistryUpdater) :
    INotificationHandler<WorkflowDefinitionPublished>,
    INotificationHandler<WorkflowDefinitionRetracted>,
    INotificationHandler<WorkflowDefinitionVersionRetracted>,
    INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionsDeleted>,
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
        return UpdateDefinition(notification.WorkflowDefinition.Id, notification.WorkflowDefinition.Options.UsableAsActivity);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionVersionRetracted notification, CancellationToken cancellationToken)
    { 
        return UpdateDefinition(notification.WorkflowDefinition.Id, notification.WorkflowDefinition.Options.UsableAsActivity);
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
        foreach (var definition in notification.WorkflowDefinitions)
        {
            await UpdateDefinition(definition.Id, definition.Options.UsableAsActivity);
        }
    }

    private Task UpdateDefinition(string id, bool? usableAsActivity)
    {
        // Once a workflow has been published, it should remain in the activity registry unless no longer being marked as an activity.
        if (usableAsActivity.GetValueOrDefault())
            return workflowDefinitionActivityRegistryUpdater.AddToRegistry(id);

        workflowDefinitionActivityRegistryUpdater.RemoveDefinitionVersionFromRegistry(id);
        return Task.CompletedTask;
    }
}