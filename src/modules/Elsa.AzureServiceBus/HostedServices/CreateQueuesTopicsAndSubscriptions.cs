using Elsa.AzureServiceBus.Contracts;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.AzureServiceBus.HostedServices;

/// <summary>
/// A blocking hosted service that creates queues, topics and subscriptions.
/// </summary>
[UsedImplicitly]
public class CreateQueuesTopicsAndSubscriptions(IServiceScopeFactory scopeFactory) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IServiceBusInitializer>();
        await initializer.InitializeAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}