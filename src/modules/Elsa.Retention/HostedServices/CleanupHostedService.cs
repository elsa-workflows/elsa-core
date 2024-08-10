using Elsa.Retention.Jobs;
using Elsa.Retention.Options;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Retention.HostedServices;

/// <summary>
/// Periodically wipes workflow instances and their execution logs.
/// </summary>
public class CleanupHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<CleanupHostedService> _logger;
    private readonly TimeSpan _interval;

    /// <summary>
    /// Creates new Cleanup hosted service
    /// </summary>
    /// <param name="options"></param>
    /// <param name="serviceScopeFactory"></param>
    /// <param name="logger"></param>
    public CleanupHostedService(IOptions<CleanupOptions> options, IServiceScopeFactory serviceScopeFactory, ILogger<CleanupHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _interval = options.Value.SweepInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            CleanupJob job = scope.ServiceProvider.GetRequiredService<CleanupJob>();

            IDistributedLockProvider distributedLockProvider = scope.ServiceProvider.GetRequiredService<IDistributedLockProvider>();
            
            await Task.Delay(_interval, stoppingToken);
            await using IDistributedSynchronizationHandle handle = await distributedLockProvider.AcquireLockAsync(nameof(CleanupHostedService), cancellationToken: stoppingToken);
            
            try
            {
                await job.ExecuteAsync(stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to perform cleanup this time around. Next cleanup attempt will happen in {Interval}", _interval);
            }
        }
    }
}