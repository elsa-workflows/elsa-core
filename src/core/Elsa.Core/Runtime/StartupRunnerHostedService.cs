using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Elsa.Runtime
{
    public class StartupRunnerHostedService : IHostedService
    {
        private readonly IStartupRunner startupRunner;

        public StartupRunnerHostedService(IStartupRunner startupRunner)
        {
            this.startupRunner = startupRunner;
        }

        public async Task StartAsync(CancellationToken cancellationToken) => 
            await startupRunner.StartupAsync(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}