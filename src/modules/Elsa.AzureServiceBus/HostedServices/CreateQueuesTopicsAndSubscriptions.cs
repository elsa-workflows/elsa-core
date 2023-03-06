using Elsa.AzureServiceBus.Contracts;
using Microsoft.Extensions.Hosting;

namespace Elsa.AzureServiceBus.HostedServices;

/// <summary>
/// A blocking hosted service that creates queues, topics and subscriptions.
/// </summary>
public class CreateQueuesTopicsAndSubscriptions : IHostedService
{
    private readonly IServiceBusInitializer _serviceBusInitializer;
    /// <summary>
    /// Constructor.
    /// </summary>
    public CreateQueuesTopicsAndSubscriptions(IServiceBusInitializer serviceBusInitializer) => _serviceBusInitializer = serviceBusInitializer;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => _serviceBusInitializer.InitializeAsync(cancellationToken);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}