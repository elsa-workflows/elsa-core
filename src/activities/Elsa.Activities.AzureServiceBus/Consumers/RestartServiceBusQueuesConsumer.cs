using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Events;
using Rebus.Handlers;

namespace Elsa.Activities.AzureServiceBus.Consumers
{
    public class RestartServiceBusQueuesConsumer : IHandleMessages<WorkflowDefinitionPublished>, IHandleMessages<WorkflowDefinitionRetracted>
    {
        private readonly IServiceBusQueuesStarter _serviceBusQueuesStarter;
        public RestartServiceBusQueuesConsumer(IServiceBusQueuesStarter serviceBusQueuesStarter) => _serviceBusQueuesStarter = serviceBusQueuesStarter;
        public Task Handle(WorkflowDefinitionPublished message) => _serviceBusQueuesStarter.CreateWorkersAsync();
        public Task Handle(WorkflowDefinitionRetracted message) => _serviceBusQueuesStarter.CreateWorkersAsync();
    }
}