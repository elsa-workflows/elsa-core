using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.AzureServiceBus.StartupTasks
{
    public class StartServiceBusSubscription : IStartupTask
    {
        private readonly IWorkflowBlueprintReflector _workflowBlueprintReflector;
        private readonly ITopicMessageReceiverFactory _messageReceiverFactory;
        private readonly IServiceProvider _serviceProvider;

        public StartServiceBusSubscription(IWorkflowBlueprintReflector workflowBlueprintReflector, ITopicMessageReceiverFactory messageReceiverFactory, IServiceProvider serviceProvider)
        {
            _workflowBlueprintReflector = workflowBlueprintReflector;
            _messageReceiverFactory = messageReceiverFactory;
            _serviceProvider = serviceProvider;
        }

        public int Order => 2000;

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var cancellationToken = stoppingToken;
            var entities = (await GetTopicSubscriptionNamesAsync(cancellationToken).ToListAsync(cancellationToken)).Distinct();

            foreach (var entity in entities)
            {
                var receiver = await _messageReceiverFactory.GetTopicReceiverAsync(entity.topicName, entity.subscriptionName, cancellationToken);
                ActivatorUtilities.CreateInstance<TopicWorker>(_serviceProvider, receiver);
            }
        }

        private async IAsyncEnumerable<(string topicName, string subscriptionName)> GetTopicSubscriptionNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var workflowRegistry = _serviceProvider.GetRequiredService<IWorkflowRegistry>();
            var workflows = await workflowRegistry.ListAsync(cancellationToken);

            var query =
                from workflow in workflows
                from activity in workflow.Activities
                where activity.Type == nameof(AzureServiceBusTopicMessageReceived)
                select workflow;

            foreach (var workflow in query)
            {
                var workflowBlueprintWrapper = await _workflowBlueprintReflector.ReflectAsync(_serviceProvider, workflow, cancellationToken);

                foreach (var activity in workflowBlueprintWrapper.Filter<AzureServiceBusTopicMessageReceived>())
                {
                    var topicName = await activity.GetPropertyValueAsync(x => x.TopicName, cancellationToken);
                    var subscriptionName = await activity.GetPropertyValueAsync(x => x.SubscriptionName, cancellationToken);
                    yield return (topicName, subscriptionName)!;
                }
            }
        }
    }
}