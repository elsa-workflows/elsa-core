using Elsa.Management.Contracts;
using Microsoft.Extensions.Hosting;

namespace Elsa.Runtime.HostedServices;

public class RegisterDescriptorsHostedService : IHostedService
{
    private readonly IActivityRegistryPopulator _activityRegistryPopulator;
    private readonly ITriggerRegistryPopulator _triggerRegistryPopulator;

    public RegisterDescriptorsHostedService(IActivityRegistryPopulator activityRegistryPopulator, ITriggerRegistryPopulator triggerRegistryPopulator)
    {
        _activityRegistryPopulator = activityRegistryPopulator;
        _triggerRegistryPopulator = triggerRegistryPopulator;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var activityRegistryTask = _activityRegistryPopulator.PopulateRegistryAsync(cancellationToken).AsTask();
        var triggerRegistryTask = _triggerRegistryPopulator.PopulateRegistryAsync(cancellationToken).AsTask();

        await Task.WhenAll(activityRegistryTask, triggerRegistryTask);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}