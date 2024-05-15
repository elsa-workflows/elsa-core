using Elsa.Mediator.Contracts;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Notifications;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Handlers;

/// <summary>
/// Refreshes the <see cref="IActivityRegistry"/> for the <see cref="WorkflowDefinitionActivityProvider"/> provider whenever an <see cref="WorkflowDefinition"/> is published, retracted or deleted.
/// </summary>
[PublicAPI]
public class RefreshActivityRegistry(IActivityRegistryPopulator activityRegistryPopulator) :
    INotificationHandler<WorkflowDefinitionPublished>,
    INotificationHandler<WorkflowDefinitionRetracted>,
    INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionsDeleted>,
    INotificationHandler<WorkflowDefinitionCreated>,
    INotificationHandler<WorkflowDefinitionVersionDeleted>,
    INotificationHandler<WorkflowDefinitionVersionsDeleted>
{
    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken)
    {
        return UpdateDefinition(notification.WorkflowDefinition.Id, notification.WorkflowDefinition.Options.UsableAsActivity);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken)
    { 
        activityRegistryPopulator.RemoveDefinitionFromRegistry(typeof(WorkflowDefinitionActivityProvider), notification.WorkflowDefinition.Id, cancellationToken);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken)
    { 
        activityRegistryPopulator.RemoveDefinitionFromRegistry(typeof(WorkflowDefinitionActivityProvider), notification.DefinitionId, cancellationToken);
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionsDeleted notification, CancellationToken cancellationToken)
    {
        foreach (string id in notification.DefinitionIds)
        {
            activityRegistryPopulator.RemoveDefinitionFromRegistry(typeof(WorkflowDefinitionActivityProvider), id, cancellationToken);
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
        activityRegistryPopulator.RemoveDefinitionFromRegistry(typeof(WorkflowDefinitionActivityProvider), notification.WorkflowDefinition.Id, cancellationToken);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionVersionsDeleted notification, CancellationToken cancellationToken)
    {
        foreach (string id in notification.Ids)
        {
            activityRegistryPopulator.RemoveDefinitionVersionFromRegistry(typeof(WorkflowDefinitionActivityProvider), id, cancellationToken);
        }

        return Task.CompletedTask;
    }

    private Task UpdateDefinition(string id, bool? usableAsActivity)
    {
        if (usableAsActivity.GetValueOrDefault())
            return activityRegistryPopulator.AddToRegistry(typeof(WorkflowDefinitionActivityProvider), id);

        activityRegistryPopulator.RemoveDefinitionFromRegistry(typeof(WorkflowDefinitionActivityProvider), id);
        return Task.CompletedTask;
    }
}