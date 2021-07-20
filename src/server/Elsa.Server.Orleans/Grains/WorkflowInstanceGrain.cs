using System.Threading;
using System.Threading.Tasks;
using Elsa.Server.Orleans.Grains.Contracts;
using Elsa.Services;
using MediatR;
using Orleans;
using Orleans.Concurrency;

namespace Elsa.Server.Orleans.Grains
{
    [StatelessWorker]
    public class WorkflowInstanceGrain : Grain, IWorkflowInstanceGrain
    {
        private readonly IMediator _mediator;
        public WorkflowInstanceGrain(IMediator mediator) => _mediator = mediator;
        public async Task ExecuteWorkflowAsync(ExecuteWorkflowInstanceRequest request, CancellationToken cancellationToken = default) => await _mediator.Send(request, cancellationToken);
    }
}