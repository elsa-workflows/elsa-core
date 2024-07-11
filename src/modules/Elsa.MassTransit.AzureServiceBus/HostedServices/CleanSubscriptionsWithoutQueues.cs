using Elsa.MassTransit.AzureServiceBus.Commands;
using Elsa.MassTransit.AzureServiceBus.Options;
using Elsa.Mediator.Contracts;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.MassTransit.AzureServiceBus.HostedServices;

/// <summary>
/// Represents a hosted service that cleans up subscriptions without queues in Azure Service Bus.
/// </summary>
public class CleanSubscriptionsWithoutQueues(IServiceProvider serviceProvider, IOptions<SubscriptionCleanupOptions> subscriptionCleanupOptions) : IHostedService, IDisposable
{
    private Timer? _timer;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Start the service a bit later to make sure it is not running simultaneously with the Heartbeat on startup.
        _timer = new Timer(CleanUpSubscriptions, null, TimeSpan.FromMinutes(2), subscriptionCleanupOptions.Value.Interval);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _timer?.Dispose();
    }

    private void CleanUpSubscriptions(object? state)
    {
        _ = Task.Run(async () => await CleanUpSubscriptionsAsync());
    }

    private async Task CleanUpSubscriptionsAsync()
    {
        using var scope = serviceProvider.CreateScope();
        var lockProvider = scope.ServiceProvider.GetRequiredService<IDistributedLockProvider>();
        var commandSender = scope.ServiceProvider.GetRequiredService<ICommandSender>();
        
        const string lockKey = "SubscriptionCleanupService";
        await using var monitorLock = await lockProvider.TryAcquireLockAsync(lockKey, TimeSpan.Zero);
        if (monitorLock == null)
            return;
        
        await commandSender.SendAsync(new CleanupSubscriptions());
    }
}