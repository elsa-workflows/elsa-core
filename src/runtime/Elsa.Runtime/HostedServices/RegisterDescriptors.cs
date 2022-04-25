using Elsa.Management.Services;
using Microsoft.Extensions.Hosting;

namespace Elsa.Runtime.HostedServices;

public class RegisterDescriptors : IHostedService
{
    private readonly IActivityRegistryPopulator _activityRegistryPopulator;
    public RegisterDescriptors(IActivityRegistryPopulator activityRegistryPopulator) => _activityRegistryPopulator = activityRegistryPopulator;
    public async Task StartAsync(CancellationToken cancellationToken) => await _activityRegistryPopulator.PopulateRegistryAsync(cancellationToken).AsTask();
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}