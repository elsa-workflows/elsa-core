using Elsa.MassTransit.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using Distributed = Elsa.MassTransit.Messages;

namespace Elsa.MassTransit.Handlers;

/// Represents a handler for distributed workflow definition notifications.
public class DistributedWorkflowDefinitionNotificationsHandler(IDistributedWorkflowDefinitionEventsDispatcher distributedEventsDispatcher) :
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
    public Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken) =>
        distributedEventsDispatcher.DispatchAsync(new Distributed.WorkflowDefinitionPublished(notification.WorkflowDefinition.Id,
            notification.WorkflowDefinition.Options.UsableAsActivity.GetValueOrDefault()), cancellationToken);

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken) =>
        distributedEventsDispatcher.DispatchAsync(new Distributed.WorkflowDefinitionRetracted(notification.WorkflowDefinition.Id, 
                notification.WorkflowDefinition.Options.UsableAsActivity.GetValueOrDefault()), cancellationToken);

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionVersionRetracted notification, CancellationToken cancellationToken) => 
        distributedEventsDispatcher.DispatchAsync(new Distributed.WorkflowDefinitionVersionRetracted(notification.WorkflowDefinition.Id, 
            notification.WorkflowDefinition.Options.UsableAsActivity.GetValueOrDefault()), cancellationToken);

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken) => 
        distributedEventsDispatcher.DispatchAsync(new Distributed.WorkflowDefinitionDeleted(notification.DefinitionId), cancellationToken);

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionsDeleted notification, CancellationToken cancellationToken) => 
        distributedEventsDispatcher.DispatchAsync(new Distributed.WorkflowDefinitionsDeleted(notification.DefinitionIds), cancellationToken);

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionVersionDeleted notification, CancellationToken cancellationToken) => 
        distributedEventsDispatcher.DispatchAsync(new Distributed.WorkflowDefinitionVersionDeleted(notification.WorkflowDefinition.Id), cancellationToken);

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionVersionsDeleted notification, CancellationToken cancellationToken) => 
        distributedEventsDispatcher.DispatchAsync(new Distributed.WorkflowDefinitionVersionsDeleted(notification.Ids), cancellationToken);

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionVersionsUpdated notification, CancellationToken cancellationToken)
    {
        var updates = notification.WorkflowDefinitions.Select(x =>
            new Distributed.WorkflowDefinitionVersionUpdate(x.Id, x.DefinitionId, x.IsPublished, x.Options.UsableAsActivity.GetValueOrDefault()));
        await distributedEventsDispatcher.DispatchAsync(new Distributed.WorkflowDefinitionVersionsUpdated(updates), cancellationToken);
    }
}