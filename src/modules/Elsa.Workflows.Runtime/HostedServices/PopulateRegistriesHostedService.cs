using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Options;
using JetBrains.Annotations;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.HostedServices;

/// <summary>
/// Updates the workflow store from <see cref="IWorkflowProvider"/> implementations, creates triggers and updates the <see cref="IActivityRegistry"/>.
/// </summary>
[UsedImplicitly]
public class PopulateRegistriesHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DistributedLockingOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="PopulateRegistriesHostedService"/> class.
    /// </summary>
    public PopulateRegistriesHostedService(IServiceScopeFactory scopeFactory, IOptions<DistributedLockingOptions> options)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
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