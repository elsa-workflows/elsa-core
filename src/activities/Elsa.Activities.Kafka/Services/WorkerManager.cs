using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Kafka.Bookmarks;
using Elsa.Activities.Kafka.Configuration;
using Elsa.Activities.Kafka.Helpers;
using Elsa.Models;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rebus.Extensions;
using static System.String;

namespace Elsa.Activities.Kafka.Services
{
    public class WorkerManager : IWorkerManager
    {
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly IServiceProvider _serviceProvider;
        private readonly ICollection<Worker> _workers;
        private readonly ILogger _logger;
        private readonly IBookmarkSerializer _bookmarkSerializer;
        private readonly KafkaOptions _kafkaOptions;
        
        public WorkerManager(
            IServiceProvider serviceProvider,
            ILogger<WorkerManager> logger,
            IBookmarkSerializer bookmarkSerializer,
            KafkaOptions? kafkaOptions)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _bookmarkSerializer = bookmarkSerializer;
            _workers = new List<Worker>();
            _kafkaOptions = kafkaOptions ?? new KafkaOptions();
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
                    await GetOrCreateWorkerAsync(trigger.WorkflowDefinitionId, CreateConfigurationFromBookmark(bookmark, trigger.ActivityId), cancellationToken);
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
                    await GetOrCreateWorkerAsync(bookmark.WorkflowInstanceId, CreateConfigurationFromBookmark(bookmarkModel, bookmark.ActivityId), cancellationToken);
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

        /// <summary>
        /// Get or create a worker if a topic and a connectionString are provided.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="configuration"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task GetOrCreateWorkerAsync(string tag, KafkaConfiguration configuration, CancellationToken cancellationToken)
        {
            try
            {
                var worker = _workers.FirstOrDefault(x => x.Topic == configuration.Topic && x.Group == configuration.Group);

                // Create worker if not found and a topic and connectionString are provided.
                if (worker is null && !IsNullOrEmpty(configuration.Topic) && !IsNullOrEmpty(configuration.ConnectionString))
                {
                    worker = ActivatorUtilities.CreateInstance<Worker>(
                        _serviceProvider,
                        configuration.Topic,
                        configuration.Group ?? "",
                        new Client(configuration, _kafkaOptions),
                        (Func<Worker, IClient, Task>)(async (w, c) => await RemoveAndRespawnWorkerAsync(w, c, tag, configuration)));

                    _logger.LogDebug("Created worker for {QueueOrTopic}", worker.Topic);
                    _workers.Add(worker);
                    _logger.LogDebug("Starting worker for {QueueOrTopic}", worker.Topic);
                    await worker.StartAsync(cancellationToken);
                }

                // Tag worker.
                worker?.Tags.Add(tag);
            }
            catch (Exception e)
            {
                if (configuration.Group == null)
                    _logger.LogWarning(e, "Failed to create a receiver for {Queue}", configuration.Topic);
                else
                    _logger.LogWarning(e, "Failed to create a receiver for {Topic} and group {Subscription}", configuration.Topic, configuration.Group);

                throw;
            }
        }

        private async Task RemoveAndRespawnWorkerAsync(Worker worker, IClient c, string tag, KafkaConfiguration configuration)
        {
            await RemoveWorkerAsync(worker);

            _logger.LogDebug("Respawning worker for {QueueOrTopic}", worker.Topic);
            await GetOrCreateWorkerAsync(tag, configuration, CancellationToken.None);
        }

        private async Task RemoveWorkerAsync(Worker worker)
        {
            _logger.LogDebug("Disposing worker for {QueueOrTopic}", worker.Topic);
            _workers.Remove(worker);
            await worker.DisposeAsync();
        }

        private IEnumerable<Bookmark> Filter(IEnumerable<Bookmark> triggers)
        {
            var modeType = typeof(MessageReceivedBookmark).GetSimpleAssemblyQualifiedName();
            return triggers.Where(x => x.ModelType == modeType);
        }

        private IEnumerable<Trigger> Filter(IEnumerable<Trigger> triggers)
        {
            var bookmarkType = typeof(MessageReceivedBookmark).GetSimpleAssemblyQualifiedName();
            return triggers.Where(x => x.ModelType == bookmarkType);
        }

        private KafkaConfiguration CreateConfigurationFromBookmark(MessageReceivedBookmark bookmark, string activityId)
        {
            var connectionString = bookmark.ConnectionString;
            var topic = bookmark.Topic;
            var group = bookmark.Group;
            var headers = bookmark.Headers;
            var clientId = KafkaClientConfigurationHelper.GetClientId(activityId);
            var autoOffsetReset = bookmark.AutoOffsetReset;
            var ignoreHeaders = bookmark.IgnoreHeaders;
            return new KafkaConfiguration(connectionString!, topic!, group!, headers, clientId, autoOffsetReset, ignoreHeaders);
        }
    }
}