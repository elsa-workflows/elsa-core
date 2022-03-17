using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.File.Services;
using Elsa.HostedServices;

namespace Elsa.Activities.File.StartupTasks
{
    public class StartFileSystemWatchers : IScopedBackgroundService
    {
        private readonly FileSystemWatchersStarter _starter;
        private readonly IServiceProvider _serviceProvider;

        public StartFileSystemWatchers(FileSystemWatchersStarter starter, IServiceProvider serviceProvider)
        {
            _starter = starter;
            _serviceProvider = serviceProvider;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken) => await _starter.CreateAndAddWatchersAsync(_serviceProvider, stoppingToken);
    }
}
