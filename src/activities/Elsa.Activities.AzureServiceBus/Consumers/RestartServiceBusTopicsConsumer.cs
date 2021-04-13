using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Events;
using Rebus.Handlers;

namespace Elsa.Activities.AzureServiceBus.Consumers
{
    public class RestartServiceBusTopicsConsumer : IHandleMessages<WorkflowDefinitionPublished>, IHandleMessages<WorkflowDefinitionRetracted>
    {
        private readonly IServiceBusTopicsStarter _serviceBusTopicsStarter;
        public RestartServiceBusTopicsConsumer(IServiceBusTopicsStarter serviceBusTopicsStarter) => _serviceBusTopicsStarter = serviceBusTopicsStarter;
        public Task Handle(WorkflowDefinitionPublished message) => _serviceBusTopicsStarter.CreateWorkersAsync();
        public Task Handle(WorkflowDefinitionRetracted message) => _serviceBusTopicsStarter.CreateWorkersAsync();
    }
}