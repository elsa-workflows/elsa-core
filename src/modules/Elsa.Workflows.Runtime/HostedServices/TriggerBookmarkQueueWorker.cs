using Elsa.Workflows.Runtime.Options;
using JetBrains.Annotations;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.HostedServices;

/// Periodically signals the bookmark queue processor to check for new items. This is a reliability measure that ensures stimuli never gets missed.
[UsedImplicitly]
public class TriggerBookmarkQueueWorker(IBookmarkQueueWorker bookmarkQueueWorker, IBookmarkQueueSignaler signaler, IServiceScopeFactory scopeFactory, IOptions<DistributedLockingOptions> options) : BackgroundService
{
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        bookmarkQueueWorker.Start();
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        bookmarkQueueWorker.Stop();
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            await using var scope = scopeFactory.CreateAsyncScope();
            var distributedLockProvider = scope.ServiceProvider.GetRequiredService<IDistributedLockProvider>();
            var lockAcquisitionTimeout = options.Value.LockAcquisitionTimeout;
            await using (await distributedLockProvider.AcquireLockAsync(nameof(TriggerBookmarkQueueWorker), lockAcquisitionTimeout, stoppingToken))
                signaler.Trigger();
        }
    }
}