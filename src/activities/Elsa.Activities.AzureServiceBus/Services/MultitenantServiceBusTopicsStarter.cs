using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public class MultitenantServiceBusTopicsStarter : ServiceBusTopicsStarter
    {
        private readonly ITenantStore _tenantStore;

        public MultitenantServiceBusTopicsStarter(
            ITopicMessageReceiverFactory receiverFactory,
            IServiceScopeFactory scopeFactory,
            ILogger<ServiceBusTopicsStarter> logger,
            ITenantStore tenantStore) : base (receiverFactory, scopeFactory, logger)
        {
            _tenantStore = tenantStore;
        }

        public override async Task CreateWorkersAsync(CancellationToken stoppingToken)
        {
            var cancellationToken = stoppingToken;
            await DisposeExistingWorkersAsync();

            foreach (var tenant in _tenantStore.GetTenants())
            {
                using var scope = _scopeFactory.CreateScopeForTenant(tenant);

                var entities = (await GetTopicSubscriptionNamesAsync(cancellationToken, scope.ServiceProvider).ToListAsync(cancellationToken)).Distinct();

                foreach (var entity in entities)
                    await CreateAndAddWorkerAsync(scope.ServiceProvider, entity.topicName, entity.subscriptionName, cancellationToken);
            }
        }
    }
}