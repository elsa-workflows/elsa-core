using JetBrains.Annotations;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Secrets.Management.HostedService;

[UsedImplicitly]
public class ExpiredSecretsHostedService(IOptions<SecretManagementOptions> options, IDistributedLockProvider distributedLockProvider, IServiceScopeFactory scopeFactory, ILogger<ExpiredSecretsHostedService> logger) : BackgroundService
{
    private Timer _timer = default!;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Get the configured sweep interval from the options, and use it to periodically sweep expired secrets.
        var sweepInterval = options.Value.SweepInterval;
        
        // Set up a timer that will sweep expired secrets at the configured interval.
        _timer = new Timer(SweepExpiredSecrets, null, sweepInterval, sweepInterval);
        
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Change(Timeout.Infinite, 0);
        _timer.Dispose();
        return base.StopAsync(cancellationToken);
    }

    private async void SweepExpiredSecrets(object? state)
    {
        // Acquire a distributed lock to ensure that only one instance of the hosted service is running at any given time.
        await using var distributedLock = await distributedLockProvider.TryAcquireLockAsync("expired-secrets-sweep");
        
        // If the lock could not be acquired, return a completed task.
        if (distributedLock == null)
        {
            logger.LogInformation("Another instance of the expired secrets hosted service is already running. Exiting...");
            return;
        }
        
        // Sweep expired secrets here.
        logger.LogInformation("Sweeping expired secrets...");
        
        using var scope = scopeFactory.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<IExpiredSecretsUpdater>();
        await updater.UpdateExpiredSecretsAsync();
        
        logger.LogInformation("Expired secrets have been swept.");
    }
}