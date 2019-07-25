using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Runtime
{
    public class StartupRunner : IStartupRunner
    {
        private readonly IEnumerable<IStartupTask> startupTasks;

        public StartupRunner(IEnumerable<IStartupTask> startupTasks)
        {
            this.startupTasks = startupTasks;
        }
        
        public async Task StartupAsync(CancellationToken cancellationToken = default)
        {
            foreach (var startupTask in startupTasks)
            {
                await startupTask.ExecuteAsync(cancellationToken);
            }
        }
    }
}