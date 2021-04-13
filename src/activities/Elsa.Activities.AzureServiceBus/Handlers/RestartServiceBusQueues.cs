using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Events;
using MediatR;

namespace Elsa.Activities.AzureServiceBus.Handlers
{
    public class RestartServiceBusQueues : INotificationHandler<WorkflowDefinitionPublished>, INotificationHandler<WorkflowDefinitionRetracted>
    {
        private readonly IServiceBusQueuesStarter _serviceBusQueuesStarter;
        public RestartServiceBusQueues(IServiceBusQueuesStarter serviceBusQueuesStarter) => _serviceBusQueuesStarter = serviceBusQueuesStarter;
        public Task Handle(WorkflowDefinitionPublished notification, CancellationToken cancellationToken) => _serviceBusQueuesStarter.CreateWorkersAsync(cancellationToken);
        public Task Handle(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken) => _serviceBusQueuesStarter.CreateWorkersAsync(cancellationToken);
    }
}