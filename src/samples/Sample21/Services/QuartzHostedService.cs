using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace Sample21.Services
{
    public class QuartzHostedService : IHostedService
    {
        private readonly IScheduler scheduler;

        public QuartzHostedService(IScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await scheduler.Start(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await scheduler.Shutdown(true, cancellationToken);
        }
    }
}