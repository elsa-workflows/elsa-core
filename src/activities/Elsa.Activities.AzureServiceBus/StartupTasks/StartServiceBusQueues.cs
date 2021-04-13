using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.AzureServiceBus.StartupTasks
{
    public class StartServiceBusQueues : IStartupTask
    {
        private readonly IWorkflowBlueprintReflector _workflowBlueprintReflector;
        private readonly IMessageReceiverFactory _messageReceiverFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StartServiceBusQueues> _logger;

        public StartServiceBusQueues(IWorkflowBlueprintReflector workflowBlueprintReflector, IMessageReceiverFactory messageReceiverFactory, IServiceProvider serviceProvider, ILogger<StartServiceBusQueues> logger)
        {
            _workflowBlueprintReflector = workflowBlueprintReflector;
            _messageReceiverFactory = messageReceiverFactory;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public int Order => 2000;

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var cancellationToken = stoppingToken;
            var queueNames = (await GetQueueNamesAsync(cancellationToken).ToListAsync(cancellationToken)).Distinct();

            foreach (var queueName in queueNames)
            {
                var receiver = await _messageReceiverFactory.GetReceiverAsync(queueName, cancellationToken);
                ActivatorUtilities.CreateInstance<QueueWorker>(_serviceProvider, receiver);
            }
        }

        private async IAsyncEnumerable<string> GetQueueNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var workflowRegistry = _serviceProvider.GetRequiredService<IWorkflowRegistry>();
            var workflows = await workflowRegistry.ListAsync(cancellationToken);

            var query =
                from workflow in workflows
                from activity in workflow.Activities
                where activity.Type == nameof(AzureServiceBusQueueMessageReceived)
                select workflow;

            foreach (var workflow in query)
            {
                var workflowBlueprintWrapper = await _workflowBlueprintReflector.ReflectAsync(_serviceProvider, workflow, cancellationToken);

                foreach (var activity in workflowBlueprintWrapper.Filter<AzureServiceBusQueueMessageReceived>())
                {
                    var queueName = await activity.GetPropertyValueAsync(x => x.QueueName, cancellationToken);

                    if (string.IsNullOrWhiteSpace(queueName))
                    {
                        _logger.LogWarning(
                            "Encountered a queue name that is null or empty in activity {ActivityType}:{ActivityId} in workflow {WorkflowDefinitionId}", 
                            activity.ActivityBlueprint.Type, 
                            activity.ActivityBlueprint.Id,
                            workflow.Id);
                        continue;
                    }

                    yield return queueName!;
                }
            }
        }
    }
}