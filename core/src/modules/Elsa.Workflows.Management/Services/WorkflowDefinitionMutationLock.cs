using Elsa.Common.DistributedHosting;
using Elsa.Common.Multitenancy;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Management;

/// <summary>
/// Coordinates workflow authoring and deletion cascades through the configured distributed lock provider.
/// </summary>
public static class WorkflowDefinitionMutationLock
{
    /// <summary>
    /// Holds the tenant's workflow definition mutation lock until the caller finishes its operation.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle> AcquireAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var provider = services.GetRequiredService<IDistributedLockProvider>();
        var tenant = services.GetRequiredService<ITenantAccessor>();
        var timeout = services.GetRequiredService<IOptions<DistributedLockingOptions>>().Value.LockAcquisitionTimeout;
        return provider.AcquireLockAsync($"Elsa:WorkflowDefinitionMutation:{tenant.TenantId}", timeout, cancellationToken);
    }
}
