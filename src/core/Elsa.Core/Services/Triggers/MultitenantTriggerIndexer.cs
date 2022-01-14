using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.MultiTenancy;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Services.Triggers
{
    public class MultitenantTriggerIndexer : TriggerIndexer
    {
        private readonly ITenantStore _tenantStore;

        public MultitenantTriggerIndexer(
            IMediator mediator,
            ILogger<MultitenantTriggerIndexer> logger,
            IServiceScopeFactory scopeFactory,
            ITenantStore tenantStore) : base(mediator, logger, scopeFactory)
        {
            _tenantStore = tenantStore;
        }

        public override async Task IndexTriggersAsync(CancellationToken cancellationToken = default)
        {
            foreach (var tenant in _tenantStore.GetTenants())
            {
                using var scope = _scopeFactory.CreateScopeForTenant(tenant);

                await IndexTriggersInternalAsync(scope.ServiceProvider, cancellationToken);
            }

            await _mediator.Publish(new MultitenantTriggerIndexingFinished(), cancellationToken);
        }
    }
}