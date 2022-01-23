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
    public record QueueWorkerKey(string Tag, string QueueName);

    // TODO: Look for a way to merge ServiceBusQueuesStarter with ServiceBusTopicsStarter - there's a lot of overlap.
    public class ServiceBusQueuesStarter : IServiceBusQueuesStarter
    {
        private readonly IQueueMessageReceiverClientFactory _messageReceiverClientFactory;
        private readonly IBookmarkSerializer _bookmarkSerializer;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly IDictionary<QueueWorkerKey, QueueWorker> _workers;
        private readonly SemaphoreSlim _semaphore = new(1);

        public ServiceBusQueuesStarter(
            IQueueMessageReceiverClientFactory messageReceiverClientFactory,
            IBookmarkSerializer bookmarkSerializer,
            IServiceProvider serviceProvider,
            ILogger<ServiceBusQueuesStarter> logger)
        {
            _messageReceiverClientFactory = messageReceiverClientFactory;
            _bookmarkSerializer = bookmarkSerializer;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _workers = new Dictionary<QueueWorkerKey, QueueWorker>();
        }

        public async Task CreateWorkersAsync(IEnumerable<Trigger> triggers, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            var filteredTriggers = Filter(triggers).ToList();

            try
            {
                foreach (var trigger in filteredTriggers)
                {
                    var bookmark = _bookmarkSerializer.Deserialize<QueueMessageReceivedBookmark>(trigger.Model);
                    await CreateAndAddWorkerAsync(trigger.WorkflowDefinitionId, bookmark.QueueName, cancellationToken);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task CreateWorkersAsync(IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            var filteredBookmarks = Filter(bookmarks).ToList();

            try
            {
                foreach (var bookmark in filteredBookmarks)
                {
                    var bookmarkModel = _bookmarkSerializer.Deserialize<QueueMessageReceivedBookmark>(bookmark.Model);
                    await CreateAndAddWorkerAsync(bookmark.WorkflowInstanceId, bookmarkModel.QueueName, cancellationToken);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task RemoveWorkersAsync(IEnumerable<Trigger> triggers, CancellationToken cancellationToken = default)
        {
            var workflowDefinitionIds = Filter(triggers).Select(x => x.WorkflowDefinitionId).Distinct().ToList();
            await RemoveWorkersAsync(workflowDefinitionIds);
        }

        public async Task RemoveWorkersAsync(IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default)
        {
            var workflowInstanceIds = Filter(bookmarks).Select(x => x.WorkflowInstanceId).Distinct().ToList();
            await RemoveWorkersAsync(workflowInstanceIds);
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

        private async Task RemoveWorkerAsync(KeyValuePair<QueueWorkerKey, QueueWorker> worker)
        {
            await worker.Value.DisposeAsync();
            _workers.Remove(worker);
        }

        private async Task CreateAndAddWorkerAsync(string tag, string queueName, CancellationToken cancellationToken)
        {
            try
            {
                var key = new QueueWorkerKey(tag, queueName);
                var worker = _workers.ContainsKey(key) ? _workers[key] : default;

                if (worker == null)
                {
                    var receiver = await _messageReceiverClientFactory.GetReceiverAsync(queueName, cancellationToken);
                    worker = ActivatorUtilities.CreateInstance<QueueWorker>(_serviceProvider, tag, receiver, (Func<IReceiverClient, Task>)DisposeReceiverAsync);
                    _workers.Add(key, worker);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to create a receiver for queue {QueueName}", queueName);
            }
        }

        private async Task DisposeReceiverAsync(IReceiverClient messageReceiver) => await _messageReceiverClientFactory.DisposeReceiverAsync(messageReceiver);

        private IEnumerable<Trigger> Filter(IEnumerable<Trigger> triggers)
        {
            var bookmarkType = typeof(QueueMessageReceivedBookmark).GetSimpleAssemblyQualifiedName();
            return triggers.Where(x => x.ModelType == bookmarkType);
        }

        private IEnumerable<Bookmark> Filter(IEnumerable<Bookmark> triggers)
        {
            var modeType = typeof(QueueMessageReceivedBookmark).GetSimpleAssemblyQualifiedName();
            return triggers.Where(x => x.ModelType == modeType);
        }
    }
}