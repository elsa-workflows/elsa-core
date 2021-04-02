using System.Threading;
using System.Threading.Tasks;
using Elsa.Dispatch;
using Elsa.Persistence;
using Elsa.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elsa.Server.Hangfire.Jobs
{
    public class WorkflowInstanceJob
    {
        private readonly IMediator _mediator;
        public WorkflowInstanceJob(IMediator mediator) => _mediator = mediator;
        public async Task ExecuteAsync(ExecuteWorkflowInstanceRequest request, CancellationToken cancellationToken = default) => await _mediator.Send(request, cancellationToken);
    }
}