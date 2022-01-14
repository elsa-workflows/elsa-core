using Elsa.Events;
using Elsa.WorkflowTesting.Events;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq.Testing
{
    public class ConfigureRabbitMqActivitiesForTestHandler : INotificationHandler<WorkflowExecuting>, INotificationHandler<WorkflowFaulted>, INotificationHandler<WorkflowCompleted>, INotificationHandler<WorkflowTestExecutionStopped>
    {
        private readonly IRabbitMqTestQueueManager _rabbitMqTestQueueManager;

        public ConfigureRabbitMqActivitiesForTestHandler(IRabbitMqTestQueueManager rabbitMqTestQueueManager)
        {
            _rabbitMqTestQueueManager = rabbitMqTestQueueManager;
        }

        public async Task Handle(WorkflowExecuting notification, CancellationToken cancellationToken)
        {

            var isTest = Convert.ToBoolean(notification.WorkflowExecutionContext.WorkflowInstance.GetMetadata("isTest"));

            if (!isTest) return;

            var workflowBlueprint = notification.WorkflowExecutionContext.WorkflowBlueprint;

            if (!workflowBlueprint.Activities.Any(x => x.Type == nameof(RabbitMqMessageReceived))) return;

            var workflowId = notification.WorkflowExecutionContext.WorkflowBlueprint.Id;
            var workflowInstanceId = notification.WorkflowExecutionContext.WorkflowInstance.Id;


            await _rabbitMqTestQueueManager.CreateTestWorkersAsync(workflowId, workflowInstanceId, cancellationToken);
        }

        public Task Handle(WorkflowFaulted notification, CancellationToken cancellationToken)
        {
            return HandleTestWorkflowExecutionFinished(notification, cancellationToken);
        }

        public Task Handle(WorkflowCompleted notification, CancellationToken cancellationToken)
        {
            return HandleTestWorkflowExecutionFinished(notification, cancellationToken);
        }

        public Task Handle(WorkflowTestExecutionStopped notification, CancellationToken cancellationToken)
        {
            _rabbitMqTestQueueManager.TryDisposeTestWorkersAsync(notification.WorkflowInstanceId);

            return Task.CompletedTask;
        }

        private async Task HandleTestWorkflowExecutionFinished(WorkflowNotification notification, CancellationToken cancellationToken)
        {
            var isTest = Convert.ToBoolean(notification.WorkflowExecutionContext.WorkflowInstance.GetMetadata("isTest"));

            if (!isTest) return;

            var workflowBlueprint = notification.WorkflowExecutionContext.WorkflowBlueprint;

            if (!workflowBlueprint.Activities.Any(x => x.Type == nameof(RabbitMqMessageReceived))) return;

            var workflowInstanceId = notification.WorkflowExecutionContext.WorkflowInstance.Id;

            await _rabbitMqTestQueueManager.TryDisposeTestWorkersAsync(workflowInstanceId);
        }
    }
}