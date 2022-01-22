using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Bookmarks;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.Triggers;
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
        private readonly IBookmarkSerializer _bookmarkSerializer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ServiceBusTopicsStarter> _logger;
        private readonly ICollection<TopicWorker> _workers;

        public ServiceBusTopicsStarter(
            ITopicMessageReceiverFactory receiverFactory,
            IServiceScopeFactory scopeFactory,
            IServiceProvider serviceProvider,
            ILogger<ServiceBusTopicsStarter> logger,
            IBookmarkSerializer bookmarkSerializer)
        {
            _receiverFactory = receiverFactory;
            _scopeFactory = scopeFactory;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _bookmarkSerializer = bookmarkSerializer;
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
                var worker = ActivatorUtilities.CreateInstance<TopicWorker>(_serviceProvider, receiver, (Func<IReceiverClient, Task>)DisposeReceiverAsync);
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
            var triggerStore = scope.ServiceProvider.GetRequiredService<ITriggerStore>();
            var triggers = await triggerStore.FindManyAsync(TriggerModelTypeSpecification.For<TopicMessageReceivedBookmark>(), cancellationToken: cancellationToken);

            foreach (var trigger in triggers)
            {
                var bookmark = _bookmarkSerializer.Deserialize<TopicMessageReceivedBookmark>(trigger.Model);
                var topicName = bookmark.TopicName;
                var subscriptionName = bookmark.SubscriptionName;
                yield return (topicName, subscriptionName)!;
            }
        }
    }
}