using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.Mqtt.Services
{
    public class MultitenantMqttTopicsStarter : MqttTopicsStarter
    {
        private readonly ITenantStore _tenantStore;

        public MultitenantMqttTopicsStarter(
            IMessageReceiverClientFactory receiverFactory,
            IServiceScopeFactory scopeFactory,
            ILogger<MultitenantMqttTopicsStarter> logger,
            ITenantStore tenantStore) : base(receiverFactory, scopeFactory, logger)
        {
            _tenantStore = tenantStore;
        }

        public override async Task CreateWorkersAsync(CancellationToken cancellationToken)
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
    }
}