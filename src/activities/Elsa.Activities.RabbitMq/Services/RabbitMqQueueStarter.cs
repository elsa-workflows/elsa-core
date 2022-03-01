using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.RabbitMq.Bookmarks;
using Elsa.Activities.RabbitMq.Configuration;
using Elsa.Activities.RabbitMq.Helpers;
using Elsa.Models;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.RabbitMq.Services
{
    public class RabbitMqQueueStarter : IRabbitMqQueueStarter
    {
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly ICollection<Worker> _workers;
        private readonly IMessageReceiverClientFactory _messageReceiverClientFactory;
        private readonly ILogger _logger;
        private readonly IBookmarkSerializer _bookmarkSerializer;

        public RabbitMqQueueStarter(
            IMessageReceiverClientFactory messageReceiverClientFactory,
            ILogger<RabbitMqQueueStarter> logger,
            IBookmarkSerializer bookmarkSerializer)
        {
            _messageReceiverClientFactory = messageReceiverClientFactory;
            _logger = logger;
            _bookmarkSerializer = bookmarkSerializer;
            _workers = new List<Worker>();
        }

        public async Task CreateWorkersAsync(IReadOnlyCollection<Trigger> triggers, IServiceProvider services, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                foreach (var trigger in triggers)
                {
                    var bookmark = _bookmarkSerializer.Deserialize<MessageReceivedBookmark>(trigger.Model);
                    var clientId = RabbitMqClientConfigurationHelper.GetClientId(trigger.ActivityId);
                    var clientConfiguration = CreateConfigurationFromBookmark(bookmark, clientId);
                    await CreateWorkersAsync(clientConfiguration, services, cancellationToken);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task CreateWorkersAsync(IReadOnlyCollection<Bookmark> bookmarks, IServiceProvider services, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                foreach (var bookmark in bookmarks)
                {
                    var bookmarkModel = _bookmarkSerializer.Deserialize<MessageReceivedBookmark>(bookmark.Model);
                    var clientId = RabbitMqClientConfigurationHelper.GetClientId(bookmark.ActivityId);
                    var clientConfiguration = CreateConfigurationFromBookmark(bookmarkModel, clientId);
                    await CreateWorkersAsync(clientConfiguration, services, cancellationToken);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task CreateWorkersAsync(RabbitMqBusConfiguration configuration, IServiceProvider services, CancellationToken cancellationToken)
        {
            try
            {
                if (!_workers.Any(x => x.Id == configuration.ClientId))
                {
                    _workers.Add(await CreateWorkerAsync(configuration, services, cancellationToken));
                }

            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to create a receiver for routing key {RoutingKey}", configuration.RoutingKey);
            }
        }

        public async Task<Worker> CreateWorkerAsync(RabbitMqBusConfiguration configuration, IServiceProvider services, CancellationToken cancellationToken)
        {
            var receiver = await _messageReceiverClientFactory.GetReceiverAsync(configuration, cancellationToken);
            return ActivatorUtilities.CreateInstance<Worker>(services, receiver, (Func<IClient, Task>)DisposeWorkerAsync);
        }

        public async Task RemoveWorkersAsync(IReadOnlyCollection<Trigger> triggers, CancellationToken cancellationToken = default)
        {
            var activityIds = triggers.Select(x => x.ActivityId).Distinct().ToList();
            await RemoveWorkersAsync(activityIds);
        }

        public async Task RemoveWorkersAsync(IReadOnlyCollection<Bookmark> bookmarks, CancellationToken cancellationToken = default)
        {
            var activityIds = bookmarks.Select(x => x.ActivityId).Distinct().ToList();
            await RemoveWorkersAsync(activityIds);
        }

        private async Task RemoveWorkersAsync(IEnumerable<string> activityIds)
        {
            var workers =
                 from worker in _workers
                 from activityId in activityIds
                 where worker.Id == activityId
                 select worker;

            foreach (var worker in workers.ToList())
                await RemoveWorkerAsync(worker);
        }

        private async Task RemoveWorkerAsync(Worker worker)
        {
            await worker.DisposeAsync();
            _workers.Remove(worker);
        }

        private async Task DisposeWorkerAsync(IClient messageReceiver) => await _messageReceiverClientFactory.DisposeReceiverAsync(messageReceiver);

        private RabbitMqBusConfiguration CreateConfigurationFromBookmark(MessageReceivedBookmark bookmark, string clientId)
        {
            var connectionString = bookmark.ConnectionString;
            var exchangeName = bookmark.ExchangeName;
            var routingKey = bookmark.RoutingKey;
            var headers = bookmark.Headers;

            return new RabbitMqBusConfiguration(connectionString!, exchangeName!, routingKey!, headers, clientId);
        }
    }
}