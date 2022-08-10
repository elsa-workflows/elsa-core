using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Activities.MqttMessageReceived;
using Elsa.Events;
using Elsa.WorkflowTesting.Events;
using MediatR;

namespace Elsa.Activities.Mqtt.Testing.Handlers
{
    public class ConfigureMqttActivitiesForTestHandler : INotificationHandler<WorkflowExecuting>, INotificationHandler<WorkflowFaulted>, INotificationHandler<WorkflowCompleted>, INotificationHandler<WorkflowTestExecutionStopped>
    {
        private readonly IMqttTestClientManager _mqttTestClientManager;

        public ConfigureMqttActivitiesForTestHandler(IMqttTestClientManager mqttTestClientManager)
        {
            _mqttTestClientManager = mqttTestClientManager;
        }

        public async Task Handle(WorkflowExecuting notification, CancellationToken cancellationToken)
        {

            var isTest = Convert.ToBoolean(notification.WorkflowExecutionContext.WorkflowInstance.GetMetadata("isTest"));

            if (!isTest) return;

            var workflowBlueprint = notification.WorkflowExecutionContext.WorkflowBlueprint;

            if (!workflowBlueprint.Activities.Any(x => x.Type == nameof(MqttMessageReceived))) return;

            var workflowId = workflowBlueprint.Id;
            var workflowInstanceId = notification.WorkflowExecutionContext.WorkflowInstance.Id;


            await _mqttTestClientManager.CreateTestWorkersAsync(workflowId, workflowInstanceId, cancellationToken);
        }

        public async Task Handle(WorkflowFaulted notification, CancellationToken cancellationToken)
        {
            await HandleTestWorkflowExecutionFinished(notification, cancellationToken);
        }

        public async Task Handle(WorkflowCompleted notification, CancellationToken cancellationToken)
        {
            await HandleTestWorkflowExecutionFinished(notification, cancellationToken);
        }

        public async Task Handle(WorkflowTestExecutionStopped notification, CancellationToken cancellationToken)
        {
            await _mqttTestClientManager.DisposeTestWorkersAsync(notification.WorkflowInstanceId);
        }

        private async Task HandleTestWorkflowExecutionFinished(WorkflowNotification notification, CancellationToken cancellationToken)
        {
            var isTest = Convert.ToBoolean(notification.WorkflowExecutionContext.WorkflowInstance.GetMetadata("isTest"));

            if (!isTest) return;

            var workflowBlueprint = notification.WorkflowExecutionContext.WorkflowBlueprint;

            if (!workflowBlueprint.Activities.Any(x => x.Type == nameof(MqttMessageReceived))) return;

            var workflowInstanceId = notification.WorkflowExecutionContext.WorkflowInstance.Id;

            await _mqttTestClientManager.DisposeTestWorkersAsync(workflowInstanceId);
        }
    }
}