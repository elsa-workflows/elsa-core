using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Bookmarks;
using Elsa.Activities.Mqtt.Helpers;
using Elsa.Activities.Mqtt.Options;
using Elsa.Models;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.Mqtt.Services
{
    public class MqttTopicsStarter : IMqttTopicsStarter
    {
        private readonly IMessageReceiverClientFactory _receiverFactory;
        private readonly IBookmarkSerializer _bookmarkSerializer;
        private readonly ILogger<MqttTopicsStarter> _logger;
        private readonly ICollection<Worker> _workers;
        private readonly SemaphoreSlim _semaphore = new(1);

        public MqttTopicsStarter(
            IMessageReceiverClientFactory receiverFactory,
            IBookmarkSerializer bookmarkSerializer,
            ILogger<MqttTopicsStarter> logger)
        {
            _receiverFactory = receiverFactory;
            _bookmarkSerializer = bookmarkSerializer;
            _logger = logger;
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
                    var clientId = MqttClientConfigurationHelper.GetClientId(trigger.ActivityId);
                    var clientOptions = CreateClientOptionsFromBookmark(bookmark, clientId);
                    await CreateWorkersAsync(clientOptions, services, cancellationToken);
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
                    var clientId = MqttClientConfigurationHelper.GetClientId(bookmark.ActivityId);
                    var clientOptions = CreateClientOptionsFromBookmark(bookmarkModel, clientId);
                    await CreateWorkersAsync(clientOptions, services, cancellationToken);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task CreateWorkersAsync(MqttClientOptions options, IServiceProvider services, CancellationToken cancellationToken)
        {
            try
            {
                if (!_workers.Any(x => x.Id == options.ClientId))
                {
                    _workers.Add(await CreateWorkerAsync(options, services, cancellationToken));
                }

            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to create a receiver for topic {Topic}", options.Topic);
            }
        }

        public async Task<Worker> CreateWorkerAsync(MqttClientOptions clientOptions, IServiceProvider services, CancellationToken cancellationToken)
        {
            var receiver = await _receiverFactory.GetReceiverAsync(clientOptions, cancellationToken);
            return ActivatorUtilities.CreateInstance<Worker>(services, receiver, (Func<IMqttClientWrapper, Task>)DisposeReceiverAsync);
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

        private async Task DisposeReceiverAsync(IMqttClientWrapper messageReceiver) => await _receiverFactory.DisposeReceiverAsync(messageReceiver);

        private MqttClientOptions CreateClientOptionsFromBookmark(MessageReceivedBookmark bookmark, string clientId) => new (bookmark.Topic, bookmark.Host, bookmark.Port, bookmark.Username, bookmark.Password, bookmark.Qos, clientId);
    }
}