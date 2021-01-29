using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.Extensions.Logging;

namespace Elsa.Runtime
{
    public class StartupRunner : IStartupRunner
    {
        private readonly ILogger<StartupRunner> _logger;
        private readonly ICollection<IStartupTask> _startupTasks;

        public StartupRunner(IEnumerable<IStartupTask> startupTasks, ILogger<StartupRunner> logger)
        {
            _logger = logger;
            _startupTasks = startupTasks.OrderBy(x => x.Order).ToList();
        }

        public async Task StartupAsync(CancellationToken cancellationToken = default)
        {
            foreach (var startupTask in _startupTasks)
            {
                _logger.LogInformation("Running startup task {StartupTaskName}", startupTask.GetType().Name);
                await startupTask.ExecuteAsync(cancellationToken);
            }
        }
    }
}