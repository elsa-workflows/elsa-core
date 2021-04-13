using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Events;
using MediatR;

namespace Elsa.Activities.AzureServiceBus.Handlers
{
    public class RestartServiceBusTopics : INotificationHandler<WorkflowDefinitionPublished>, INotificationHandler<WorkflowDefinitionRetracted>
    {
        private readonly IServiceBusTopicsStarter _serviceBusTopicsStarter;
        public RestartServiceBusTopics(IServiceBusTopicsStarter serviceBusTopicsStarter) => _serviceBusTopicsStarter = serviceBusTopicsStarter;
        public Task Handle(WorkflowDefinitionPublished notification, CancellationToken cancellationToken) => _serviceBusTopicsStarter.CreateWorkersAsync(cancellationToken);
        public Task Handle(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken) => _serviceBusTopicsStarter.CreateWorkersAsync(cancellationToken);
    }
}