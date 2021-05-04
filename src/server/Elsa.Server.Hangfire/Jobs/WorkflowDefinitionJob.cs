using System.Threading;
using System.Threading.Tasks;
using Elsa.Dispatch;
using MediatR;

namespace Elsa.Server.Hangfire.Jobs
{
    public class WorkflowDefinitionJob
    {
        private readonly IMediator _mediator;
        public WorkflowDefinitionJob(IMediator mediator) => _mediator = mediator;
        public async Task ExecuteAsync(ExecuteWorkflowDefinitionRequest request, CancellationToken cancellationToken = default) => await _mediator.Send(request, cancellationToken);
    }
}