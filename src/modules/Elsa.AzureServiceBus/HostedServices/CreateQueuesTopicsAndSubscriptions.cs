using Elsa.AzureServiceBus.Services;
using Microsoft.Extensions.Hosting;

namespace Elsa.AzureServiceBus.HostedServices;

/// <summary>
/// A blocking hosted service that creates queues, topics and subscriptions.
/// </summary>
public class CreateQueuesTopicsAndSubscriptions : IHostedService
{
    private readonly IServiceBusInitializer _serviceBusInitializer;
    public CreateQueuesTopicsAndSubscriptions(IServiceBusInitializer serviceBusInitializer) => _serviceBusInitializer = serviceBusInitializer;
    public Task StartAsync(CancellationToken cancellationToken) => _serviceBusInitializer.InitializeAsync(cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}