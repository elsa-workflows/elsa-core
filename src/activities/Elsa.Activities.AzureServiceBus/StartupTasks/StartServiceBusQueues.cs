using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Activities;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using IServiceBusFactory = Elsa.Activities.AzureServiceBus.Services.IServiceBusFactory;

namespace Elsa.Activities.AzureServiceBus.StartupTasks
{
    public class StartServiceBusQueues : IStartupTask
    {
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowBlueprintReflector _workflowBlueprintReflector;
        private readonly IServiceBusFactory _serviceBusFactory;
        private readonly IServiceProvider _serviceProvider;

        public StartServiceBusQueues(IWorkflowRegistry workflowRegistry, IWorkflowBlueprintReflector workflowBlueprintReflector, IServiceBusFactory serviceBusFactory, IServiceProvider serviceProvider)
        {
            _workflowRegistry = workflowRegistry;
            _workflowBlueprintReflector = workflowBlueprintReflector;
            _serviceBusFactory = serviceBusFactory;
            _serviceProvider = serviceProvider;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var queueNames = await GetQueueNamesAsync(cancellationToken).ToListAsync(cancellationToken);

            foreach (var queueName in queueNames)
            {
                var receiver = await _serviceBusFactory.GetReceiverAsync(queueName, cancellationToken);
                var worker = ActivatorUtilities.CreateInstance<QueueWorker>(_serviceProvider, receiver);
                
                await worker.StartAsync(cancellationToken);
            }
        }

        private async IAsyncEnumerable<string> GetQueueNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var workflows = await _workflowRegistry.GetWorkflowsAsync(cancellationToken).ToListAsync(cancellationToken);

            var query =
                from workflow in workflows
                from activity in workflow.Activities
                where activity.Type == nameof(AzureServiceBusMessageReceived)
                select workflow;

            foreach (var workflow in query)
            {
                var workflowBlueprintWrapper = await _workflowBlueprintReflector.ReflectAsync(workflow, cancellationToken);

                foreach (var activity in workflowBlueprintWrapper.Filter<AzureServiceBusMessageReceived>())
                {
                    var queueName = await activity.GetPropertyValueAsync(x => x.QueueName, cancellationToken);
                    yield return queueName;
                }
            }
        }
    }
}