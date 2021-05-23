using System.Threading.Tasks;
using Elsa.Events;
using MediatR;
using Rebus.Handlers;

namespace Elsa.Activities.Temporal.Common.Consumers
{
    public class RemoveScheduledTriggersConsumer : IHandleMessages<WorkflowDefinitionPublished>, IHandleMessages<WorkflowDefinitionRetracted>, IHandleMessages<WorkflowDefinitionDeleted>
    {
        private readonly IMediator _mediator;

        public RemoveScheduledTriggersConsumer(IMediator mediator)
        {
            _mediator = mediator;
        }
        
        public Task Handle(WorkflowDefinitionPublished message) => _mediator.Publish(message);

        public Task Handle(WorkflowDefinitionRetracted message) => _mediator.Publish(message);

        public Task Handle(WorkflowDefinitionDeleted message) => _mediator.Publish(message);
    }
}