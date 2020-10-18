using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Messages;
using Elsa.Services;
using Rebus.ServiceProvider;

namespace Elsa.StartupTasks
{
    public class StartServiceBusTask : IStartupTask
    {
        private readonly IServiceProvider _serviceProvider;
        public StartServiceBusTask(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
        
        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            _serviceProvider.UseRebus(x => x.Subscribe<RunWorkflow>());
            return Task.CompletedTask;
        }
    }
    
}