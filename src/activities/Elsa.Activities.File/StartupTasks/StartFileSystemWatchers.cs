using Elsa.Activities.File.Services;
using Elsa.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.File.StartupTasks
{
    public class StartFileSystemWatchers : IStartupTask
    {
        private readonly FileSystemWatchersStarter _starter;

        public StartFileSystemWatchers(FileSystemWatchersStarter starter) => _starter = starter;

        public int Order => 2000;

        public Task ExecuteAsync(CancellationToken cancellationToken = default) => _starter.CreateAndAddWatchersAsync(cancellationToken);
    }
}
