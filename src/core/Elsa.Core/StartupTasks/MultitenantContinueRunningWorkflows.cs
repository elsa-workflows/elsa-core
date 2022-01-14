using System.Threading;
using System.Threading.Tasks;
using Elsa.MultiTenancy;
using Elsa.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IDistributedLockProvider = Elsa.Services.IDistributedLockProvider;

namespace Elsa.StartupTasks
{
    /// <summary>
    /// If there are workflows in the Running state while the server starts, it means the workflow instance never finished execution, e.g. because the workflow host terminated.
    /// This startup task resumes these workflows.
    /// </summary>
    public class MultitenantContinueRunningWorkflows : ContinueRunningWorkflows
    {
        private readonly ITenantStore _tenantStore;

        public MultitenantContinueRunningWorkflows(
            IDistributedLockProvider distributedLockProvider,
            ElsaOptions elsaOptions,
            ILogger<MultitenantContinueRunningWorkflows> logger,
            IServiceScopeFactory scopeFactory,
            ITenantStore tenantStore): base (distributedLockProvider, scopeFactory, elsaOptions, logger)
        {
            _tenantStore = tenantStore;
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await using var handle = await _distributedLockProvider.AcquireLockAsync(GetType().Name, _elsaOptions.DistributedLockTimeout, cancellationToken);

            if (handle == null)
                return;

            foreach (var tenant in _tenantStore.GetTenants())
            {
                using var scope = _scopeFactory.CreateScopeForTenant(tenant);

                await ResumeIdleWorkflows(scope.ServiceProvider, cancellationToken);
                await ResumeRunningWorkflowsAsync(scope.ServiceProvider, cancellationToken);
            }
        }
    }
}