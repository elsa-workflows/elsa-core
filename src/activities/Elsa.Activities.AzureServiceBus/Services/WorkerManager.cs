using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Bookmarks;
using Elsa.Models;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rebus.Extensions;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public class WorkerManager : IWorkerManager
    {
        private readonly IBookmarkSerializer _bookmarkSerializer;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WorkerManager> _logger;
        private readonly ICollection<Worker> _workers;
        private readonly SemaphoreSlim _semaphore = new(1);

        public WorkerManager(
            IServiceProvider serviceProvider,
            IBookmarkSerializer bookmarkSerializer,
            ILogger<WorkerManager> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _bookmarkSerializer = bookmarkSerializer;
            _workers = new Collection<Worker>();
        }

        public async Task CreateWorkersAsync(IReadOnlyCollection<Trigger> triggers, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            var filteredTriggers = Filter(triggers).ToList();

            try
            {
                foreach (var trigger in filteredTriggers)
                {
                    var bookmark = _bookmarkSerializer.Deserialize<MessageReceivedBookmark>(trigger.Model);
                    await CreateAndAddWorkerAsync(trigger.WorkflowDefinitionId, bookmark.QueueOrTopic, bookmark.Subscription, cancellationToken);
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
                    var bookmarkModel = _bookmarkSerializer.Deserialize<MessageReceivedBookmark>(bookmark.Model);
                    await CreateAndAddWorkerAsync(bookmark.WorkflowInstanceId, bookmarkModel.QueueOrTopic, bookmarkModel.Subscription, cancellationToken);
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
                where worker.Tag == workflowId
                select worker;

            foreach (var worker in workers.ToList())
            {
                await worker.DisposeAsync();
                _workers.Remove(worker);
            }
        }

        public async Task RemoveWorkersAsync(IReadOnlyCollection<Bookmark> bookmarks, CancellationToken cancellationToken = default)
        {
            var workflowInstanceIds = Filter(bookmarks).Select(x => x.WorkflowInstanceId).Distinct().ToList();
            await RemoveWorkersAsync(workflowInstanceIds);
        }

        private async Task CreateAndAddWorkerAsync(string tag, string queueOrTopic, string? subscription, CancellationToken cancellationToken)
        {
            try
            {
                var worker = _workers.FirstOrDefault(x => x.Tag == tag && x.QueueOrTopic == queueOrTopic && x.Subscription == subscription);

                if (worker == null)
                {
                    worker = ActivatorUtilities.CreateInstance<Worker>(_serviceProvider, queueOrTopic, subscription ?? "", tag);
                    _workers.Add(worker);
                    await worker.StartAsync(cancellationToken);
                }
            }
            catch (Exception e)
            {
                if(subscription == null)
                    _logger.LogWarning(e, "Failed to create a receiver for {Queue}", queueOrTopic);
                else
                    _logger.LogWarning(e, "Failed to create a receiver for {Topic} and subscription {Subscription}", queueOrTopic, subscription);
            }
        }

        private async Task RemoveWorkersAsync(IEnumerable<string> tags)
        {
            var workers =
                from worker in _workers
                from tag in tags
                where worker.Tag == tag
                select worker;

            foreach (var worker in workers.ToList())
                await RemoveWorkerAsync(worker);
        }

        private async Task RemoveWorkerAsync(Worker worker)
        {
            await worker.DisposeAsync();
            _workers.Remove(worker);
        }

        private IEnumerable<Trigger> Filter(IEnumerable<Trigger> triggers)
        {
            var bookmarkType = typeof(MessageReceivedBookmark).GetSimpleAssemblyQualifiedName();
            return triggers.Where(x => x.ModelType == bookmarkType);
        }

        private IEnumerable<Bookmark> Filter(IEnumerable<Bookmark> triggers)
        {
            var modeType = typeof(MessageReceivedBookmark).GetSimpleAssemblyQualifiedName();
            return triggers.Where(x => x.ModelType == modeType);
        }
    }
}