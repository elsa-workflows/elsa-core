using System.Threading;
using System.Threading.Tasks;
using Elsa.MultiTenancy;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.Temporal.Common.HostedServices
{
    /// <summary>
    /// Starts jobs based on workflow instances blocked on a Timer, Cron or StartAt activity.
    /// </summary>
    public class MultitenantStartJobs : StartJobs
    {
        private readonly ITenantStore _tenantStore;

        public MultitenantStartJobs(IDistributedLockProvider distributedLockProvider, ILogger<StartJobs> logger, IServiceScopeFactory scopeFactory, ITenantStore tenantStore): base(distributedLockProvider, logger, scopeFactory) => _tenantStore = tenantStore;

        protected override async Task ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            foreach (var tenant in _tenantStore.GetTenants())
            {
                using var scope = _scopeFactory.CreateScopeForTenant(tenant);
                await ScheduleWorkflowsAsync(scope.ServiceProvider, cancellationToken);
            }
        }
    }
}