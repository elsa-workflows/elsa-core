using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
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
                    await GetOrCreateWorkerAsync(trigger.WorkflowDefinitionId, bookmark.QueueOrTopic, bookmark.Subscription, cancellationToken);
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
                    await GetOrCreateWorkerAsync(bookmark.WorkflowInstanceId, bookmarkModel.QueueOrTopic, bookmarkModel.Subscription, cancellationToken);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task RemoveTagsFromWorkersAsync(IReadOnlyCollection<string> tags, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                foreach (var worker in _workers.ToList())
                {
                    // Remove tags.
                    worker.Tags.RemoveWhere(tags.Contains);

                    // Remove worker if it has no more tags.
                    if (!worker.Tags.Any())
                        await RemoveWorkerAsync(worker);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task GetOrCreateWorkerAsync(string tag, string queueOrTopic, string? subscription, CancellationToken cancellationToken)
        {
            try
            {
                var worker = _workers.FirstOrDefault(x => x.QueueOrTopic == queueOrTopic && x.Subscription == subscription);

                // Create worker if not found.
                if (worker == null)
                {
                    worker = ActivatorUtilities.CreateInstance<Worker>(
                        _serviceProvider,
                        queueOrTopic,
                        subscription ?? "",
                        (Func<Worker, ProcessErrorEventArgs, Task>)(async (w, e) => await RemoveAndRespawnWorkerAsync(w, e, tag, queueOrTopic, subscription)));

                    _logger.LogDebug("Created worker for {QueueOrTopic}", worker.QueueOrTopic);
                    _workers.Add(worker);

                    _logger.LogDebug("Starting worker for {QueueOrTopic}", worker.QueueOrTopic);
                    await worker.StartAsync(cancellationToken);
                }

                // Tag worker.
                worker.Tags.Add(tag);
            }
            catch (Exception e)
            {
                if (subscription == null)
                    _logger.LogWarning(e, "Failed to create a receiver for {Queue}", queueOrTopic);
                else
                    _logger.LogWarning(e, "Failed to create a receiver for {Topic} and subscription {Subscription}", queueOrTopic, subscription);

                throw;
            }
        }

        private async Task RemoveAndRespawnWorkerAsync(Worker worker, ProcessErrorEventArgs args, string tag, string queueOrTopic, string? subscription)
        {
            _logger.LogDebug(args.Exception, "Error occurred in processor for {QueueOrTopic}. Error source: {ErrorSource}", worker.QueueOrTopic, args.ErrorSource);
            await RemoveWorkerAsync(worker);

            _logger.LogDebug("Respawning worker for {QueueOrTopic}", worker.QueueOrTopic);
            await GetOrCreateWorkerAsync(tag, queueOrTopic, subscription, args.CancellationToken);
        }

        private async Task RemoveWorkerAsync(Worker worker)
        {
            _logger.LogDebug("Disposing worker for {QueueOrTopic}", worker.QueueOrTopic);
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