using Elsa.Extensions;
using Elsa.Persistence;
using Elsa.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Elsa.Models;
using Newtonsoft.Json.Linq;
using Elsa.AzureServiceBus.Activities;
using Elsa.AzureServiceBus.Models;
using Elsa.AzureServiceBus.Notifications;

namespace Elsa.AzureServiceBus.MessageHandlers
{
    public class ServiceBusMessageReceivedHandler : INotificationHandler<ProcessMessageNotification>
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceBusMessageReceivedHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

        }

        public async Task Handle(ProcessMessageNotification notification, CancellationToken cancellationToken)
        {
            if (notification.Message != null)
            {

                using (var scope = _serviceProvider.CreateScope())
                {
                    var registry = scope.ServiceProvider.GetRequiredService<IWorkflowRegistry>();
                    var workflowInstanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
                    var workflowInvoker = scope.ServiceProvider.GetRequiredService<IWorkflowInvoker>();

                    var sbusWorkflows = await registry.ListByStartActivityAsync(nameof(ServiceBusSignalReceived), cancellationToken);

                    var haltedSbusWorkflows = await workflowInstanceStore.ListByBlockingActivityAsync<ServiceBusSignalReceived>(
                        cancellationToken: cancellationToken);

                    //get the list of workflows to start and resume from this signal
                    var workflowsToStart = Filter(sbusWorkflows, notification.ConsumerName, notification.Message.BodyType).ToList();
                    var workflowsToResume = Filter(haltedSbusWorkflows, notification.ConsumerName, notification.Message.BodyType).ToList();

                    if (workflowsToStart.Any())
                    {
                        await InvokeWorkflowsToStartAsync(workflowInvoker, cancellationToken, workflowsToStart, notification.Message);
                    }

                    if (workflowsToResume.Any())
                    {
                        await InvokeWorkflowsToResumeAsync(workflowInvoker, cancellationToken, workflowsToResume, notification.Message);
                    }





                }


            }




        }

        private IEnumerable<(WorkflowInstance, ActivityInstance)> Filter(
            IEnumerable<(WorkflowInstance, ActivityInstance)> items,
             string consumerName,
            string messageType)
        {
            return items.Where(x => IsMatch(x.Item2.State, consumerName, messageType));
        }

        private IEnumerable<(WorkflowDefinitionVersion, ActivityDefinition)> Filter(
            IEnumerable<(WorkflowDefinitionVersion, ActivityDefinition)> items,
            string consumerName,
            string messageType)
        {
            return items.Where(x => IsMatch(x.Item2.State, consumerName, messageType));
        }

        /// <summary>
        /// Checks the message type and consumer name against the activity state for a match
        /// </summary>
        /// <param name="state"></param>
        /// <param name="consumerName"></param>
        /// <param name="messageType"></param>
        /// <returns></returns>
        private bool IsMatch(JObject state, string consumerName, string messageType)
        {
            var c = ServiceBusSignalReceived.GetConsumerName(state);
            var m = ServiceBusSignalReceived.GetMessageType(state);

            return (string.IsNullOrWhiteSpace(c) || c == "*" || string.Compare(c, consumerName, true) == 0) &&
                (string.IsNullOrWhiteSpace(m) || string.Compare(m, messageType, true) == 0);
        }

        private async Task InvokeWorkflowsToStartAsync(IWorkflowInvoker workflowInvoker, CancellationToken cancellationToken,
            IEnumerable<(WorkflowDefinitionVersion, ActivityDefinition)> items, MessageBody messageBody)
        {
            foreach (var item in items)
            {
                await workflowInvoker.StartAsync(
                    item.Item1,
                    new Variables(
                        new Dictionary<string, object>
                        {
                            { ServiceBusSignalReceived.INPUT_VARIABLE_NAME, messageBody }
                        }
                    ),
                    new[] { item.Item2.Id },
                    cancellationToken: cancellationToken);
            }
        }

        private async Task InvokeWorkflowsToResumeAsync(IWorkflowInvoker workflowInvoker, CancellationToken cancellationToken, IEnumerable<(WorkflowInstance, ActivityInstance)> items, MessageBody messageBody)
        {
            foreach (var (workflowInstance, activity) in items)
            {
                await workflowInvoker.ResumeAsync(
                    workflowInstance,
                    new Variables(
                        new Dictionary<string, object>
                        {
                            { ServiceBusSignalReceived.INPUT_VARIABLE_NAME, messageBody }
                        }
                    ),
                    new[] { activity.Id },
                    cancellationToken);
            }
        }
    }
}

