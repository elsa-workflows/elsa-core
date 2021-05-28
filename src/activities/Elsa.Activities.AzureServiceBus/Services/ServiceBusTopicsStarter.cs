using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.AzureServiceBus.Services
{
    // TODO: Look for a way to merge ServiceBusQueuesStarter with ServiceBusTopicsStarter - there's a lot of overlap.
    public class ServiceBusTopicsStarter : IServiceBusTopicsStarter
    {
        private readonly ITopicMessageReceiverFactory _receiverFactory;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ServiceBusTopicsStarter> _logger;
        private readonly ICollection<TopicWorker> _workers;

        public ServiceBusTopicsStarter(
            ITopicMessageReceiverFactory receiverFactory,
            IServiceScopeFactory scopeFactory,
            IServiceProvider serviceProvider,
            ILogger<ServiceBusTopicsStarter> logger)
        {
            _receiverFactory = receiverFactory;
            _scopeFactory = scopeFactory;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _workers = new List<TopicWorker>();
        }

        public async Task CreateWorkersAsync(CancellationToken stoppingToken)
        {
            var cancellationToken = stoppingToken;
            await DisposeExistingWorkersAsync();
            var entities = (await GetTopicSubscriptionNamesAsync(cancellationToken).ToListAsync(cancellationToken)).Distinct();

            foreach (var entity in entities) 
                await CreateAndAddWorkerAsync(entity.topicName, entity.subscriptionName, cancellationToken);
        }

        private async Task CreateAndAddWorkerAsync(string topicName, string subscriptionName, CancellationToken cancellationToken)
        {
            try
            {
                var receiver = await _receiverFactory.GetTopicReceiverAsync(topicName, subscriptionName, cancellationToken);
                var worker = ActivatorUtilities.CreateInstance<TopicWorker>(_serviceProvider, receiver, (Func<IReceiverClient, Task>) DisposeReceiverAsync);
                _workers.Add(worker);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to create a receiver for topic {TopicName} and subscription {SubscriptionName}", topicName, subscriptionName);
            }
        }

        private async Task DisposeExistingWorkersAsync()
        {
            foreach (var worker in _workers.ToList())
            {
                await worker.DisposeAsync();
                _workers.Remove(worker);
            }
        }

        private async Task DisposeReceiverAsync(IReceiverClient messageReceiver) => await _receiverFactory.DisposeReceiverAsync(messageReceiver);

        private async IAsyncEnumerable<(string topicName, string subscriptionName)> GetTopicSubscriptionNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var workflowRegistry = scope.ServiceProvider.GetRequiredService<IWorkflowRegistry>();
            var workflowBlueprintReflector = scope.ServiceProvider.GetRequiredService<IWorkflowBlueprintReflector>();
            var workflowInstanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
            var workflows = await workflowRegistry.ListActiveAsync(cancellationToken);

            var query =
                from workflow in workflows
                from activity in workflow.Activities
                where activity.Type == nameof(AzureServiceBusTopicMessageReceived)
                select workflow;

            foreach (var workflow in query)
            {
                // If a workflow is not published, only consider it for processing if it has at least one non-ended workflow instance.
                if (!workflow.IsPublished && !await WorkflowHasNonFinishedWorkflowsAsync(workflow, workflowInstanceStore, cancellationToken))
                    continue;
                
                var workflowBlueprintWrapper = await workflowBlueprintReflector.ReflectAsync(scope.ServiceProvider, workflow, cancellationToken);

                foreach (var activity in workflowBlueprintWrapper.Filter<AzureServiceBusTopicMessageReceived>())
                {
                    var topicName = await activity.EvaluatePropertyValueAsync(x => x.TopicName, cancellationToken);
                    var subscriptionName = await activity.EvaluatePropertyValueAsync(x => x.SubscriptionName, cancellationToken);
                    yield return (topicName, subscriptionName)!;
                }
            }
        }
        
        private static async Task<bool> WorkflowHasNonFinishedWorkflowsAsync(IWorkflowBlueprint workflowBlueprint, IWorkflowInstanceStore workflowInstanceStore, CancellationToken cancellationToken)
        {
            var count = await workflowInstanceStore.CountAsync(new NonFinalizedWorkflowSpecification().WithWorkflowDefinition(workflowBlueprint.Id), cancellationToken);
            return count > 0;
        }
    }
}