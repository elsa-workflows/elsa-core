using Elsa.Workflows.Management.Contracts;
using Microsoft.Extensions.Hosting;

namespace Elsa.Workflows.Runtime.HostedServices;

internal class PopulateActivityRegistry : IHostedService
{
    private readonly IActivityRegistryPopulator _activityRegistryPopulator;
    public PopulateActivityRegistry(IActivityRegistryPopulator activityRegistryPopulator) => _activityRegistryPopulator = activityRegistryPopulator;
    public async Task StartAsync(CancellationToken cancellationToken) => await _activityRegistryPopulator.PopulateRegistryAsync(cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}