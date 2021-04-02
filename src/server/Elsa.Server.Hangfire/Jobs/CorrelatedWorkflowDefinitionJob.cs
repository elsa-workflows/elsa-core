using System.Threading;
using System.Threading.Tasks;
using Elsa.Dispatch;
using MediatR;

namespace Elsa.Server.Hangfire.Jobs
{
    public class CorrelatedWorkflowDefinitionJob
    {
        private readonly IMediator _mediator;
        public CorrelatedWorkflowDefinitionJob(IMediator mediator) => _mediator = mediator;
        public async Task ExecuteAsync(TriggerWorkflowsRequest request, CancellationToken cancellationToken = default) => await _mediator.Send(request, cancellationToken);
    }
}