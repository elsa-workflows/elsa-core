using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Runtime
{
    public class StartupRunner : IStartable
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

        public void Start()
        {
            // TODO: Register Startup Types the same way Activity Types are registered.
            
            foreach (var startupTaskType in _startupTaskTypes)
            {
                using var scope = _scopeFactory.CreateAsyncScope();
                var startupTask = (IStartupTask)scope.ServiceProvider.GetRequiredService(startupTaskType);
                _logger.LogInformation("Running startup task {StartupTaskName}", startupTaskType.Name);
                startupTask.ExecuteAsync().GetAwaiter().GetResult();
            }
        }
    }
}