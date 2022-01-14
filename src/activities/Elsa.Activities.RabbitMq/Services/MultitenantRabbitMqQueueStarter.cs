using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.RabbitMq.Services
{
    public class MultitenantRabbitMqQueueStarter : RabbitMqQueueStarter
    {
        private readonly ITenantStore _tenantStore;

        public MultitenantRabbitMqQueueStarter(
            IMessageReceiverClientFactory messageReceiverClientFactory,
            IServiceScopeFactory scopeFactory,
            ILogger<MultitenantRabbitMqQueueStarter> logger,
            ITenantStore tenantStore) : base(messageReceiverClientFactory, scopeFactory, logger)
        {
            _tenantStore = tenantStore;
        }

        public override async Task CreateWorkersAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                await DisposeExistingWorkersAsync();

                foreach (var tenant in _tenantStore.GetTenants())
                {
                    using var scope = _scopeFactory.CreateScopeForTenant(tenant);

                    var receiverConfigs = (await GetConfigurationsAsync<RabbitMqMessageReceived>(null, scope.ServiceProvider, cancellationToken).ToListAsync(cancellationToken)).GroupBy(c => c.GetHashCode()).Select(x => x.First());

                    foreach (var config in receiverConfigs)
                    {
                        try
                        {
                            _workers.Add(await CreateWorkerAsync(scope.ServiceProvider, config, cancellationToken));
                        }
                        catch (Exception e)
                        {
                            _logger.LogWarning(e, "Failed to create a receiver for routing key {RoutingKey}", config.RoutingKey);
                        }
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
