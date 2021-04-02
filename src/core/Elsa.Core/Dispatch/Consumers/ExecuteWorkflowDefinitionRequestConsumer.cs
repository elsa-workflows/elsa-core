using System.Threading.Tasks;
using MediatR;
using Rebus.Handlers;

namespace Elsa.Dispatch.Consumers
{
    public class ExecuteWorkflowDefinitionRequestConsumer : IHandleMessages<ExecuteWorkflowDefinitionRequest>
    {
        private readonly IMediator _mediator;
        public ExecuteWorkflowDefinitionRequestConsumer(IMediator mediator) => _mediator = mediator;
        public async Task Handle(ExecuteWorkflowDefinitionRequest message) => await _mediator.Send(message);
    }
}