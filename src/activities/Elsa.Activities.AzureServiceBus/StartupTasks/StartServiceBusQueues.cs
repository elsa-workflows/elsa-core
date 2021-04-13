using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Services;

namespace Elsa.Activities.AzureServiceBus.StartupTasks
{
    public class StartServiceBusQueues : IStartupTask
    {
        private readonly IServiceBusQueuesStarter _serviceBusQueuesStarter;
        public StartServiceBusQueues(IServiceBusQueuesStarter serviceBusQueuesStarter) => _serviceBusQueuesStarter = serviceBusQueuesStarter;
        public int Order => 2000;
        public Task ExecuteAsync(CancellationToken stoppingToken) => _serviceBusQueuesStarter.CreateWorkersAsync(stoppingToken);
    }
}