using System.Threading.Tasks;
using MediatR;
using Rebus.Handlers;

namespace Elsa.Dispatch.Consumers
{
    public class TriggerWorkflowsRequestConsumer : IHandleMessages<TriggerWorkflowsRequest>
    {
        private readonly IMediator _mediator;
        public TriggerWorkflowsRequestConsumer(IMediator mediator) => _mediator = mediator;
        public async Task Handle(TriggerWorkflowsRequest message) => await _mediator.Send(message);
    }
}