using Elsa.Common;
using Elsa.Workflows.Runtime.Options;
using JetBrains.Annotations;
using Medallion.Threading;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.HostedServices;

/// Updates the workflow store from <see cref="IWorkflowsProvider"/> implementations, creates triggers and updates the <see cref="IActivityRegistry"/>.
[UsedImplicitly]
public class PopulateRegistriesStartupTask(IDistributedLockProvider distributedLockProvider, IRegistriesPopulator registriesPopulator, IOptions<DistributedLockingOptions> options) : IStartupTask
{
    private readonly DistributedLockingOptions _options = options.Value;
    
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var lockAcquisitionTimeout = _options.LockAcquisitionTimeout;
        await using (await distributedLockProvider.AcquireLockAsync("PopulateRegistries", lockAcquisitionTimeout, cancellationToken))
        {
            await registriesPopulator.PopulateAsync(cancellationToken);
        }
    }
}