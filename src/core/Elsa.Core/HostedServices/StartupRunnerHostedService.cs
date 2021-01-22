using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.HostedServices
{
    public class StartupRunnerHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        public StartupRunnerHostedService(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var startupRunner = scope.ServiceProvider.GetRequiredService<IStartupRunner>();
            await startupRunner.StartupAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}