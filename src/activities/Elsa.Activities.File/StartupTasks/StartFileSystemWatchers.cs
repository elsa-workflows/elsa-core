using Elsa.Activities.File.Services;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Elsa.Activities.File.StartupTasks
{
    public class StartFileSystemWatchers : BackgroundService
    {
        private readonly FileSystemWatchersStarter _starter;
        public StartFileSystemWatchers(FileSystemWatchersStarter starter) => _starter = starter;
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) => await _starter.CreateAndAddWatchersAsync(stoppingToken);
    }
}
