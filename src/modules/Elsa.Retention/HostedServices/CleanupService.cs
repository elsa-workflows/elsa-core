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
    public class CleanupService : IHostedService, IAsyncDisposable
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly CleanupOptions _options;
        private readonly Timer _timer;

        public CleanupService(IOptions<CleanupOptions> options, IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _options = options.Value;
            _timer = new Timer(ExecuteAsync, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer.Change(_options.SweepInterval.ToTimeSpan(), Timeout.InfiniteTimeSpan);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync() => await _timer.DisposeAsync();

        private async void ExecuteAsync(object state)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var job = scope.ServiceProvider.GetRequiredService<CleanupJob>();
            await job.ExecuteAsync();

            _timer.Change(_options.SweepInterval.ToTimeSpan(), Timeout.InfiniteTimeSpan);
        }
    }
}