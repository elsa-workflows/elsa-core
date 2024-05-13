using Elsa.MassTransit.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using Distributed = Elsa.MassTransit.Messages;

namespace Elsa.MassTransit.Handlers
{
    /// Represents a handler for distributed workflow definition notifications.
    public class DistributedWorkflowDefinitionNotificationsHandler(IDistributedWorkflowDefinitionEventsDispatcher distributedEventsDispatcher) :
        INotificationHandler<WorkflowDefinitionPublished>,
        INotificationHandler<WorkflowDefinitionDeleted>,
        INotificationHandler<WorkflowDefinitionsDeleted>,
        INotificationHandler<WorkflowDefinitionCreated>,
        INotificationHandler<WorkflowDefinitionVersionDeleted>,
        INotificationHandler<WorkflowDefinitionVersionsDeleted>
    {
        /// <inheritdoc />
        public async Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken) =>
            await distributedEventsDispatcher.DispatchAsync(new Distributed.WorkflowDefinitionPublished(notification.WorkflowDefinition.Id,
                notification.WorkflowDefinition.Options.UsableAsActivity.GetValueOrDefault()), cancellationToken);

        /// <inheritdoc />
        public async Task HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken) => 
            await distributedEventsDispatcher.DispatchAsync(new Distributed.WorkflowDefinitionDeleted(notification.DefinitionId), cancellationToken);

        /// <inheritdoc />
        public async Task HandleAsync(WorkflowDefinitionsDeleted notification, CancellationToken cancellationToken) => 
            await distributedEventsDispatcher.DispatchAsync(new Distributed.WorkflowDefinitionsDeleted(notification.DefinitionIds), cancellationToken);

        /// <inheritdoc />
        public async Task HandleAsync(WorkflowDefinitionCreated notification, CancellationToken cancellationToken) => 
            await distributedEventsDispatcher.DispatchAsync(new Distributed.WorkflowDefinitionCreated(notification.WorkflowDefinition.Id,
                notification.WorkflowDefinition.Options.UsableAsActivity.GetValueOrDefault()), cancellationToken);

        /// <inheritdoc />
        public async Task HandleAsync(WorkflowDefinitionVersionDeleted notification, CancellationToken cancellationToken) => 
            await distributedEventsDispatcher.DispatchAsync(new Distributed.WorkflowDefinitionVersionDeleted(notification.WorkflowDefinition.Id), cancellationToken);

        /// <inheritdoc />
        public async Task HandleAsync(WorkflowDefinitionVersionsDeleted notification, CancellationToken cancellationToken) => 
            await distributedEventsDispatcher.DispatchAsync(new Distributed.WorkflowDefinitionVersionsDeleted(notification.Ids), cancellationToken);
    }
}