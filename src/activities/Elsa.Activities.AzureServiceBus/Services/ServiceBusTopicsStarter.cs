using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Bookmarks;
using Elsa.Models;
using Elsa.Services;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rebus.Extensions;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public record TopicWorkerKey(string Tag, string TopicName, string SubscriptionName);

    // TODO: Look for a way to merge ServiceBusQueuesStarter with ServiceBusTopicsStarter - there's a lot of overlap.
    public class ServiceBusTopicsStarter : IServiceBusTopicsStarter
    {
        private readonly ITopicMessageReceiverFactory _receiverFactory;
        private readonly IBookmarkSerializer _bookmarkSerializer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ServiceBusTopicsStarter> _logger;
        private readonly IDictionary<TopicWorkerKey, TopicWorker> _workers;
        private readonly SemaphoreSlim _semaphore = new(1);

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
            _workers = new Dictionary<TopicWorkerKey, TopicWorker>();
        }

        public async Task CreateWorkersAsync(IReadOnlyCollection<Trigger> triggers, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            var filteredTriggers = Filter(triggers).ToList();

            try
            {
                foreach (var trigger in filteredTriggers)
                {
                    var bookmark = _bookmarkSerializer.Deserialize<TopicMessageReceivedBookmark>(trigger.Model);
                    await CreateAndAddWorkerAsync(trigger.WorkflowDefinitionId, bookmark.TopicName, bookmark.SubscriptionName, cancellationToken);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task CreateWorkersAsync(IReadOnlyCollection<Bookmark> bookmarks, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            var filteredBookmarks = Filter(bookmarks).ToList();

            try
            {
                foreach (var bookmark in filteredBookmarks)
                {
                    var bookmarkModel = _bookmarkSerializer.Deserialize<TopicMessageReceivedBookmark>(bookmark.Model);
                    await CreateAndAddWorkerAsync(bookmark.WorkflowInstanceId, bookmarkModel.TopicName, bookmarkModel.SubscriptionName, cancellationToken);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task RemoveWorkersAsync(IReadOnlyCollection<Trigger> triggers, CancellationToken cancellationToken = default)
        {
            var workflowDefinitionIds = Filter(triggers).Select(x => x.WorkflowDefinitionId).Distinct().ToList();

            var workers =
                from worker in _workers
                from workflowId in workflowDefinitionIds
                where worker.Key.Tag == workflowId
                select worker;

            foreach (var worker in workers.ToList())
            {
                await worker.Value.DisposeAsync();
                _workers.Remove(worker);
            }
        }

        public async Task RemoveWorkersAsync(IReadOnlyCollection<Bookmark> bookmarks, CancellationToken cancellationToken = default)
        {
            var workflowInstanceIds = Filter(bookmarks).Select(x => x.WorkflowInstanceId).Distinct().ToList();
            await RemoveWorkersAsync(workflowInstanceIds);
        }

        private async Task CreateAndAddWorkerAsync(string tag, string topicName, string subscriptionName, CancellationToken cancellationToken)
        {
            try
            {
                var key = new TopicWorkerKey(tag, topicName, subscriptionName);
                var worker = _workers.ContainsKey(key) ? _workers[key] : default;

                if (worker == null)
                {
                    var receiver = await _receiverFactory.GetTopicReceiverAsync(topicName, subscriptionName, cancellationToken);
                    worker = ActivatorUtilities.CreateInstance<TopicWorker>(_serviceProvider, tag, receiver, (Func<IReceiverClient, Task>)DisposeReceiverAsync);
                    _workers.Add(key, worker);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to create a receiver for topic {TopicName} and subscription {SubscriptionName}", topicName, subscriptionName);
            }
        }

        private async Task RemoveWorkersAsync(IEnumerable<string> tags)
        {
            var workers =
                from worker in _workers
                from tag in tags
                where worker.Key.Tag == tag
                select worker;

            foreach (var worker in workers.ToList())
                await RemoveWorkerAsync(worker);
        }

        private async Task RemoveWorkerAsync(KeyValuePair<TopicWorkerKey, TopicWorker> worker)
        {
            await worker.Value.DisposeAsync();
            _workers.Remove(worker);
        }

        private async Task DisposeReceiverAsync(IReceiverClient messageReceiver) => await _receiverFactory.DisposeReceiverAsync(messageReceiver);

        private IEnumerable<Trigger> Filter(IEnumerable<Trigger> triggers)
        {
            var bookmarkType = typeof(TopicMessageReceivedBookmark).GetSimpleAssemblyQualifiedName();
            return triggers.Where(x => x.ModelType == bookmarkType);
        }

        private IEnumerable<Bookmark> Filter(IEnumerable<Bookmark> triggers)
        {
            var modeType = typeof(TopicMessageReceivedBookmark).GetSimpleAssemblyQualifiedName();
            return triggers.Where(x => x.ModelType == modeType);
        }
    }
}