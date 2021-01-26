using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

namespace Elsa.Runtime
{
    public class StartupRunner : IStartupRunner
    {
        private readonly IEnumerable<IStartupTask> _startupTasks;

        public StartupRunner(IEnumerable<IStartupTask> startupTasks)
        {
            _startupTasks = startupTasks.OrderBy(x => x.Order).ToList();
        }

        public async Task StartupAsync(CancellationToken cancellationToken = default)
        {
            foreach (var startupTask in _startupTasks) 
                await startupTask.ExecuteAsync(cancellationToken);
        }
    }
}