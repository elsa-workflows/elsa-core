using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Runtime
{
    public class StartupRunner : IStartupRunner
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StartupRunner> _logger;
        private readonly ICollection<Type> _startupTaskTypes;

        public StartupRunner(IEnumerable<IStartupTask> startupTasks, IServiceScopeFactory scopeFactory, ILogger<StartupRunner> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _startupTaskTypes = startupTasks.OrderBy(x => x.Order).Select(x => x.GetType()).ToList();
        }

        public async Task StartupAsync(CancellationToken cancellationToken = default)
        {
            // TODO: Register Startup Types the same way Activity Types are registered.
            
            foreach (var startupTaskType in _startupTaskTypes)
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var startupTask = (IStartupTask)scope.ServiceProvider.GetRequiredService(startupTaskType);
                _logger.LogInformation("Running startup task {StartupTaskName}", startupTaskType.Name);
                await startupTask.ExecuteAsync(cancellationToken);
            }
        }
    }
}