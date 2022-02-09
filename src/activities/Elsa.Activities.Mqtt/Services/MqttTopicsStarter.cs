using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Bookmarks;
using Elsa.Activities.Mqtt.Options;
using Elsa.Models;
using Elsa.MultiTenancy;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.Mqtt.Services
{
    public class MqttTopicsStarter : IMqttTopicsStarter
    {
        private readonly IMessageReceiverClientFactory _receiverFactory;
        private readonly IBookmarkSerializer _bookmarkSerializer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MqttTopicsStarter> _logger;
        private readonly ICollection<Worker> _workers;
        private readonly ITenantStore _tenantStore;

        public MqttTopicsStarter(
            IMessageReceiverClientFactory receiverFactory,
            IBookmarkSerializer bookmarkSerializer,
            IServiceScopeFactory scopeFactory,
            ILogger<MqttTopicsStarter> logger,
            ITenantStore tenantStore)
        {
            _receiverFactory = receiverFactory;
            _bookmarkSerializer = bookmarkSerializer;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _workers = new List<Worker>();
            _tenantStore = tenantStore;
        }

        public async Task CreateWorkersAsync(CancellationToken cancellationToken)
        {
            await DisposeExistingWorkersAsync();

            foreach (var tenant in _tenantStore.GetTenants())
            {
                using var scope = _scopeFactory.CreateScopeForTenant(tenant);

                var configs = (await GetConfigurationsAsync(null, scope.ServiceProvider, cancellationToken).ToListAsync(cancellationToken)).Distinct();

                foreach (var config in configs)
                {
                    try
                    {
                        _workers.Add(await CreateWorkerAsync(scope.ServiceProvider, config, cancellationToken));
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Failed to create a receiver for topic {Topic}", config.Topic);
                    }
                }
            }
        }

        public async Task<Worker> CreateWorkerAsync(IServiceProvider serviceProvider, MqttClientOptions config, CancellationToken cancellationToken)
        {
            var receiver = await _receiverFactory.GetReceiverAsync(config, cancellationToken);
            return ActivatorUtilities.CreateInstance<Worker>(serviceProvider, receiver, (Func<IMqttClientWrapper, Task>)DisposeReceiverAsync);
        }

        public async IAsyncEnumerable<MqttClientOptions> GetConfigurationsAsync(Func<Trigger, bool>? predicate, IServiceProvider serviceProvider, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var triggerFinder = serviceProvider.GetRequiredService<ITriggerFinder>();
            var triggers = await triggerFinder.FindTriggersByTypeAsync<MessageReceivedBookmark>(cancellationToken: cancellationToken);
            var filteredTriggers = predicate == null ? triggers : triggers.Where(predicate);

            foreach (var trigger in filteredTriggers)
            {
                var bookmark = _bookmarkSerializer.Deserialize<MessageReceivedBookmark>(trigger.Model);
                var topic = bookmark.Topic;
                var host = bookmark.Host;
                var port = bookmark.Port;
                var username = bookmark.Username;
                var password = bookmark.Password;
                var qos = bookmark.Qos;

                yield return new MqttClientOptions(topic!, host!, port!, username!, password!, qos);
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

        private async Task DisposeReceiverAsync(IMqttClientWrapper messageReceiver) => await _receiverFactory.DisposeReceiverAsync(messageReceiver);
    }
}