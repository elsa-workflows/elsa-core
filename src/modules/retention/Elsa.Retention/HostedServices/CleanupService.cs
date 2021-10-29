using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Retention.Jobs;
using Elsa.Retention.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Retention.HostedServices
{
    /// <summary>
    /// Periodically wipes workflow instances and their execution logs.
    /// </summary>
    public class CleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly TimeSpan _interval;

        public CleanupService(IOptions<CleanupOptions> options, IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _interval = options.Value.SweepInterval.ToTimeSpan();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var job = scope.ServiceProvider.GetRequiredService<CleanupJob>();
            
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_interval, stoppingToken);
                await job.ExecuteAsync(stoppingToken);
            }
        }
    }
}