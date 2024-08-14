using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Options;
using JetBrains.Annotations;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.HostedServices;

/// Updates the workflow store from <see cref="IWorkflowsProvider"/> implementations, creates triggers and updates the <see cref="IActivityRegistry"/>.
[UsedImplicitly]
public class PopulateRegistriesHostedService(IServiceScopeFactory scopeFactory, IOptions<DistributedLockingOptions> options) : IHostedService
{
    private readonly DistributedLockingOptions _options = options.Value;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var distributedLockProvider = scope.ServiceProvider.GetRequiredService<IDistributedLockProvider>();
        var lockAcquisitionTimeout = _options.LockAcquisitionTimeout;
        await using (await distributedLockProvider.AcquireLockAsync("PopulateRegistries", lockAcquisitionTimeout, cancellationToken))
        {
            var registriesPopulator = scope.ServiceProvider.GetRequiredService<IRegistriesPopulator>();
            await registriesPopulator.PopulateAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}