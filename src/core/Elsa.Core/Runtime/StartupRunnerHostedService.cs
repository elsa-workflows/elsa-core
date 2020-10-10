using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Elsa.Runtime
{
    public class StartupRunnerHostedService : IHostedService
    {
        private readonly IStartupRunner _startupRunner;

        public StartupRunnerHostedService(IStartupRunner startupRunner)
        {
            _startupRunner = startupRunner;
        }

        public async Task StartAsync(CancellationToken cancellationToken) => 
            await _startupRunner.StartupAsync(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}