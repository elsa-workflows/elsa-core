using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Services;

namespace Elsa.Activities.AzureServiceBus.StartupTasks
{
    public class StartServiceBusTopics : IStartupTask
    {
        private readonly IServiceBusTopicsStarter _serviceBusTopicsStarter;
        public StartServiceBusTopics(IServiceBusTopicsStarter serviceBusTopicsStarter) => _serviceBusTopicsStarter = serviceBusTopicsStarter;
        public int Order => 2000;
        public Task ExecuteAsync(CancellationToken stoppingToken) => _serviceBusTopicsStarter.CreateWorkersAsync(stoppingToken);
    }
}