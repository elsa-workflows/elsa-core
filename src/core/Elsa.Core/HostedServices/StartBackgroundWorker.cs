using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.Extensions.Hosting;

namespace Elsa.HostedServices
{
    public class StartBackgroundWorker : BackgroundService
    {
        private readonly IBackgroundWorker _backgroundWorker;
        public StartBackgroundWorker(IBackgroundWorker backgroundWorker) => _backgroundWorker = backgroundWorker;
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) => await _backgroundWorker.RunAsync(stoppingToken);
    }
}