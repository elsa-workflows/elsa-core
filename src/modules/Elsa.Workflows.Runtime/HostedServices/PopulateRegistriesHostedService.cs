using Elsa.Common.Multitenancy;
using Elsa.Workflows.Runtime.Options;
using JetBrains.Annotations;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.HostedServices;

/// Updates the workflow store from <see cref="IWorkflowsProvider"/> implementations, creates triggers and updates the <see cref="IActivityRegistry"/>.
[UsedImplicitly]
public class PopulateRegistriesHostedService(IServiceScopeFactory scopeFactory, IOptions<DistributedLockingOptions> options) : MultitenantHostedService(scopeFactory)
{
    private readonly DistributedLockingOptions _options = options.Value;

    /// <inheritdoc />
    protected override async Task StartAsync(TenantScope tenantScope, CancellationToken cancellationToken)
    {
        await PopulateRegistriesAsync(tenantScope, cancellationToken);
    }
    
    private async Task PopulateRegistriesAsync(TenantScope scope, CancellationToken cancellationToken)
    {
        var distributedLockProvider = scope.ServiceProvider.GetRequiredService<IDistributedLockProvider>();
        var lockAcquisitionTimeout = _options.LockAcquisitionTimeout;
        await using (await distributedLockProvider.AcquireLockAsync("PopulateRegistries", lockAcquisitionTimeout, cancellationToken))
        {
            var registriesPopulator = scope.ServiceProvider.GetRequiredService<IRegistriesPopulator>();
            await registriesPopulator.PopulateAsync(cancellationToken);
        }
    }
}