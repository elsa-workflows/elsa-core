using Elsa.MassTransit.Services;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;
using MassTransit;
using Distributed = Elsa.MassTransit.Messages;

namespace Elsa.MassTransit.Handlers;

/// <summary>
/// Represents a handler for distributed workflow definition notifications.
/// </summary>
[UsedImplicitly]
public class DistributedWorkflowDefinitionNotificationsHandler(IBus bus) :
    INotificationHandler<WorkflowDefinitionPublished>,
    INotificationHandler<WorkflowDefinitionRetracted>,
    INotificationHandler<WorkflowDefinitionVersionRetracted>,
    INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionsDeleted>,
    INotificationHandler<WorkflowDefinitionVersionDeleted>,
    INotificationHandler<WorkflowDefinitionVersionsDeleted>,
    INotificationHandler<WorkflowDefinitionVersionsUpdated>,
    INotificationHandler<WorkflowDefinitionsRefreshed>,
    INotificationHandler<WorkflowDefinitionsReloaded>
{
    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken)
    {
        return bus.Publish(new Distributed.WorkflowDefinitionPublished(notification.WorkflowDefinition.Id, notification.WorkflowDefinition.Options.UsableAsActivity.GetValueOrDefault()), cancellationToken);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken)
    {
        return bus.Publish(new Distributed.WorkflowDefinitionRetracted(notification.WorkflowDefinition.Id, notification.WorkflowDefinition.Options.UsableAsActivity.GetValueOrDefault()), cancellationToken);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionVersionRetracted notification, CancellationToken cancellationToken)
    {
        return bus.Publish(new Distributed.WorkflowDefinitionVersionRetracted(notification.WorkflowDefinition.Id, notification.WorkflowDefinition.Options.UsableAsActivity.GetValueOrDefault()), cancellationToken);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken)
    {
        return bus.Publish(new Distributed.WorkflowDefinitionDeleted(notification.DefinitionId), cancellationToken);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionsDeleted notification, CancellationToken cancellationToken)
    {
        return bus.Publish(new Distributed.WorkflowDefinitionsDeleted(notification.DefinitionIds), cancellationToken);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionVersionDeleted notification, CancellationToken cancellationToken)
    {
        return bus.Publish(new Distributed.WorkflowDefinitionVersionDeleted(notification.WorkflowDefinition.Id), cancellationToken);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionVersionsDeleted notification, CancellationToken cancellationToken)
    {
        return bus.Publish(new Distributed.WorkflowDefinitionVersionsDeleted(notification.Ids), cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionVersionsUpdated notification, CancellationToken cancellationToken)
    {
        var updates = notification.WorkflowDefinitions.Select(x => new Distributed.WorkflowDefinitionVersionUpdate(x.Id, x.DefinitionId, x.IsPublished, x.Options.UsableAsActivity.GetValueOrDefault()));
        await bus.Publish(new Distributed.WorkflowDefinitionVersionsUpdated(updates), cancellationToken);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionsRefreshed notification, CancellationToken cancellationToken)
    {
        // Prevent re-entrance.
        if (AmbientConsumerScope.IsWorkflowDefinitionEventsConsumer)
            return Task.CompletedTask;

        var definitionIds = notification.WorkflowDefinitionIds;
        var message = new Distributed.WorkflowDefinitionsRefreshed(definitionIds);
        return bus.Publish(message, cancellationToken);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionsReloaded notification, CancellationToken cancellationToken)
    {
        // Prevent re-entrance.
        if (AmbientConsumerScope.IsWorkflowDefinitionEventsConsumer)
            return Task.CompletedTask;

        var reloadedWorkflowDefinitions = notification.ReloadedWorkflowDefinitions;
        var message = new Distributed.WorkflowDefinitionsReloaded(reloadedWorkflowDefinitions);
        return bus.Publish(message, cancellationToken);
    }
}