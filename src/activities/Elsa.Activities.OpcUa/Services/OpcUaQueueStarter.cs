using Elsa.Activities.OpcUa.Bookmarks;
using Elsa.Activities.OpcUa.Configuration;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.OpcUa.Services
{
    public class OpcUaQueueStarter : IOpcUaQueueStarter
    {
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICollection<Worker> _workers;
        private readonly IMessageReceiverClientFactory _messageReceiverClientFactory;
        private readonly ILogger _logger;
        private readonly IBookmarkSerializer _bookmarkSerializer;

        public OpcUaQueueStarter(
            IMessageReceiverClientFactory messageReceiverClientFactory,
            IServiceScopeFactory scopeFactory,
            IServiceProvider serviceProvider,
            ILogger<OpcUaQueueStarter> logger,
            IBookmarkSerializer bookmarkSerializer)
        {
            _messageReceiverClientFactory = messageReceiverClientFactory;
            _scopeFactory = scopeFactory;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _bookmarkSerializer = bookmarkSerializer;
            _workers = new List<Worker>();
        }

        public async Task CreateWorkersAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                await DisposeExistingWorkersAsync();

                var receiverConfigs = (await GetConfigurationsAsync(cancellationToken).ToListAsync(cancellationToken)).GroupBy(c => c.GetHashCode()).Select(x => x.First());

                foreach (var config in receiverConfigs)
                {
                    try
                    {
                        _workers.Add(await CreateWorkerAsync(config, cancellationToken));
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Failed to create a receiver for routing key {RoutingKey}", "");
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<Worker> CreateWorkerAsync(OpcUaBusConfiguration config, CancellationToken cancellationToken = default)
        {
            var receiver = await _messageReceiverClientFactory.GetReceiverAsync(config, cancellationToken);
            return ActivatorUtilities.CreateInstance<Worker>(_serviceProvider, (Func<IClient, Task>)DisposeWorkerAsync, receiver);
        }

        private async Task DisposeWorkerAsync(IClient messageReceiver) => await _messageReceiverClientFactory.DisposeReceiverAsync(messageReceiver);

        public async IAsyncEnumerable<OpcUaBusConfiguration> GetConfigurationsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var triggerFinder = scope.ServiceProvider.GetRequiredService<ITriggerFinder>();
            var triggers = await triggerFinder.FindTriggersByTypeAsync<MessageReceivedBookmark>(cancellationToken: cancellationToken);

            foreach (var trigger in triggers)
            {
                var bookmarkModel = _bookmarkSerializer.Deserialize<MessageReceivedBookmark>(trigger.Model);

                var configuration = CreateConfigurationFromBookmark(bookmarkModel, trigger.ActivityId);

                yield return configuration;
            }

            var bookmarkFinder = scope.ServiceProvider.GetRequiredService<IBookmarkFinder>();
            var bookmarks = await bookmarkFinder.FindBookmarksByTypeAsync<MessageReceivedBookmark>(cancellationToken: cancellationToken);

            foreach (var bookmark in bookmarks)
            {
                var bookmarkModel = _bookmarkSerializer.Deserialize<MessageReceivedBookmark>(bookmark.Model);

                var configuration = CreateConfigurationFromBookmark(bookmarkModel, bookmark.ActivityId);

                yield return configuration;
            }

        }


        private OpcUaBusConfiguration CreateConfigurationFromBookmark(MessageReceivedBookmark bookmark, string activityId)
        {
            var connectionString = bookmark.ConnectionString;
            var Tags = bookmark.Tags;
            var clientId = $"Elsa-{activityId.ToUpper()}";

            return new OpcUaBusConfiguration(connectionString!, clientId, Tags);
        }

        private async Task DisposeExistingWorkersAsync()
        {
            foreach (var worker in _workers.ToList())
            {
                await worker.DisposeAsync();
                _workers.Remove(worker);
            }
        }
    }
}