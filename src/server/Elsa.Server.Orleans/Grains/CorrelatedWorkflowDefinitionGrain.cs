using System.Threading;
using System.Threading.Tasks;
using Elsa.Dispatch;
using Elsa.Server.Orleans.Grains.Contracts;
using MediatR;
using Orleans;
using Orleans.Concurrency;

namespace Elsa.Server.Orleans.Grains
{
    [StatelessWorker]
    public class CorrelatedWorkflowDefinitionGrain : Grain, ICorrelatedWorkflowGrain
    {
        private readonly IMediator _mediator;
        public CorrelatedWorkflowDefinitionGrain(IMediator mediator) => _mediator = mediator;
        public async Task ExecutedCorrelatedWorkflowAsync(TriggerWorkflowsRequest request, CancellationToken cancellationToken = default) => await _mediator.Send(request, cancellationToken);
    }
}