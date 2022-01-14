using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public class MultitenantServiceBusQueuesStarter : ServiceBusQueuesStarter
    {
        private readonly ITenantStore _tenantStore;

        public MultitenantServiceBusQueuesStarter(
            IQueueMessageReceiverClientFactory messageReceiverClientFactory,
            IServiceScopeFactory scopeFactory,
            ILogger<ServiceBusQueuesStarter> logger,
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

                    var queueNames = (await GetQueueNamesAsync(cancellationToken, scope.ServiceProvider).ToListAsync(cancellationToken)).Distinct();

                    foreach (var queueName in queueNames)
                        await CreateAndAddWorkerAsync(scope.ServiceProvider, queueName, cancellationToken);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}