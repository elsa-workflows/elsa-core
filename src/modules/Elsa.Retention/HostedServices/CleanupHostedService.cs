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
public class CleanupHostedService : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<CleanupHostedService> _logger;
    private readonly TimeSpan _interval;
    private bool _isStarted;

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

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<CleanupJob>();

        var distributedLockProvider = scope.ServiceProvider.GetRequiredService<IDistributedLockProvider>();
        
        while (_isStarted)
        {
            await Task.Delay(_interval, stoppingToken);
            await using var handle = await distributedLockProvider.AcquireLockAsync(nameof(CleanupHostedService), cancellationToken: stoppingToken);
            
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

    /// <inheritdoc cref="IHostedService"/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _isStarted = true;
        ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IHostedService"/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _isStarted = false;
        return Task.CompletedTask;
    }
}