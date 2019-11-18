using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace Sample21.Services
{
    public class QuartzHostedService : IHostedService
    {
        private readonly IScheduler _scheduler;

        public QuartzHostedService(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {

            await _scheduler.Start(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _scheduler.Shutdown(true, cancellationToken);
        }
    }
}