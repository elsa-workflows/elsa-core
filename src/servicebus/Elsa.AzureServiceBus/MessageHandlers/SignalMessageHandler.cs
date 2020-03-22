using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;
using Elsa.AzureServiceBus.Activities;
using Elsa.AzureServiceBus.Models;
using Elsa.AzureServiceBus.Notifications;
using Elsa.AzureServiceBus.Services;
using Elsa.Services.Models;

namespace Elsa.AzureServiceBus.MessageHandlers
{
    /// <summary>
    /// Processes any incoming signal messages received via servicebus 
    /// </summary>
    public class SignalMessageHandler : INotificationHandler<ProcessMessageNotification>
    {
        private readonly ITokenService _tokenService;
        private readonly IServiceProvider _serviceProvider;

        public SignalMessageHandler(IServiceProvider serviceProvider, ITokenService tokenService)
        {
            _serviceProvider = serviceProvider;
            _tokenService = tokenService;
        }

        public async Task Handle(ProcessMessageNotification notification, CancellationToken cancellationToken)
        {
            if (notification.Message != null)
            {
                if (notification.Message.BodyType == "ACTION_SIGNAL")
                {
                    var encodedSignal = Encoding.UTF8.GetString(notification.Message.Data);


                    if (_tokenService.TryDecryptToken(encodedSignal, out Signal signal))
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var workflowInstanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();

                            var workflowInstance = await workflowInstanceStore.GetByIdAsync(signal.WorkflowInstanceId, cancellationToken);


                            if (workflowInstance != null)
                            {
                                var workflowInvoker = scope.ServiceProvider.GetRequiredService<IWorkflowInvoker>();
                                var workflowFactory = scope.ServiceProvider.GetRequiredService<IWorkflowFactory>();
                                var workflowRegistry = scope.ServiceProvider.GetRequiredService<IWorkflowRegistry>();

                                var input = new Variables();
                                input.SetVariable(ServiceBusSignaled.INPUT_VARIABLE_NAME, signal.Name);

                                var workflowDefinition = await workflowRegistry.GetWorkflowDefinitionAsync(
                                    workflowInstance.DefinitionId,
                                    VersionOptions.SpecificVersion(workflowInstance.Version),
                                    cancellationToken);



                                var workflow = workflowFactory.CreateWorkflow(workflowDefinition, input, workflowInstance);
                                var blockingSignalActivities = Filter(workflow.BlockingActivities.ToList(), notification.ConsumerName);

                                if (blockingSignalActivities.Any())
                                {
                                    await workflowInvoker.ResumeAsync(workflow, blockingSignalActivities, cancellationToken);
                                }

                            }

                        }

                    }



                }
            }


        }

        private IEnumerable<IActivity> Filter(
           IEnumerable<IActivity> items,
            string consumerName
           )
        {
            return items.Where(x => x is ServiceBusSignaled && IsMatch(x.State, consumerName));
        }

        private bool IsMatch(JObject state, string consumerName)
        {
            var c = ServiceBusSignaled.GetConsumerName(state);

            return string.IsNullOrWhiteSpace(c) || c == "*" || string.Compare(c, consumerName, true) == 0;
        }


    }
}
