using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Runtime;

namespace Elsa.StartupTasks
{
    public class StartServiceBusTask : IStartupTask
    {
        private readonly IServiceProvider serviceProvider;
        public StartServiceBusTask(IServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;
        public Task ExecuteAsync(CancellationToken cancellationToken = default) => serviceProvider.StartElsaAsync();
    }
}