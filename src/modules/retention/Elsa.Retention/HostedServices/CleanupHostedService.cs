using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Retention.Jobs;
using Elsa.Retention.Options;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Retention.HostedServices
{
    /// <summary>
    /// Periodically wipes workflow instances and their execution logs.
    /// </summary>
    public class CleanupHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ILogger<CleanupHostedService> _logger;
        private readonly TimeSpan _interval;

        public CleanupHostedService(IOptions<CleanupOptions> options, IServiceScopeFactory serviceScopeFactory, IDistributedLockProvider distributedLockProvider, ILogger<CleanupHostedService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _distributedLockProvider = distributedLockProvider;
            _logger = logger;
            _interval = options.Value.SweepInterval.ToTimeSpan();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var job = scope.ServiceProvider.GetRequiredService<CleanupJob>();
            var started = false;

            while (!stoppingToken.IsCancellationRequested)
            {
                if (started)
                    await Task.Delay(_interval, stoppingToken);
                else
                    started = true;
                await using var handle = await _distributedLockProvider.AcquireLockAsync(nameof(CleanupHostedService), cancellationToken: stoppingToken);

                if (handle == null)
                    continue;

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
}