using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.Temporal.Common.Handlers
{
    public class MultitenantSchedulePublishedWorkflows : SchedulePublishedWorkflows
    {
        private readonly ITenantStore _tenantStore;

        public MultitenantSchedulePublishedWorkflows(ILogger<SchedulePublishedWorkflows> logger, ITenantStore tenantStore, IServiceScopeFactory scopeFactory) : base(logger, scopeFactory) => _tenantStore = tenantStore;

        public override async Task Handle(TriggerIndexingFinished notification, CancellationToken cancellationToken)
        {
            foreach (var tenant in _tenantStore.GetTenants())
            {
                using var scope = _scopeFactory.CreateScopeForTenant(tenant);
                await SchedulePublishedWorkflowsInternalAsync(scope.ServiceProvider, cancellationToken);
            }
        }
    }
}