using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using JetBrains.Annotations;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Workflows.Runtime.HostedServices;

/// <summary>
/// Updates the workflow store from <see cref="IWorkflowProvider"/> implementations, creates triggers and updates the <see cref="IActivityRegistry"/>.
/// </summary>
[UsedImplicitly]
public class PopulateRegistriesHostedService : IHostedService
{
    private readonly IDistributedLockProvider _distributedLockProvider;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PopulateRegistriesHostedService"/> class.
    /// </summary>
    public PopulateRegistriesHostedService(IDistributedLockProvider distributedLockProvider, IServiceScopeFactory scopeFactory)
    {
        _distributedLockProvider = distributedLockProvider;
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using (await _distributedLockProvider.AcquireLockAsync("PopulateRegistries", TimeSpan.FromMinutes(10), cancellationToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var registriesPopulator = scope.ServiceProvider.GetRequiredService<IRegistriesPopulator>();
            await registriesPopulator.PopulateAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}