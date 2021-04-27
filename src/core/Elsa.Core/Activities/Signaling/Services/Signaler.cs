using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Signaling.Models;
using Elsa.Dispatch;
using MediatR;

namespace Elsa.Activities.Signaling.Services
{
    public class Signaler : ISignaler
    {
        // TODO: Design multi-tenancy. 
        private const string TenantId = default;
        
        private readonly IMediator _mediator;
        private readonly IWorkflowDispatcher _workflowDispatcher;

        public Signaler(IMediator mediator, IWorkflowDispatcher workflowDispatcher)
        {
            _mediator = mediator;
            _workflowDispatcher = workflowDispatcher;
        }

        public async Task TriggerSignalAsync(string signal, object? input = default, string? workflowInstanceId = default, CancellationToken cancellationToken = default) =>
            await _mediator.Send(new TriggerWorkflowsRequest(
                    nameof(SignalReceived),
                    new SignalReceivedBookmark {Signal = signal, WorkflowInstanceId = workflowInstanceId},
                    new SignalReceivedBookmark {Signal = signal},
                    new Signal(signal, input),
                    default,
                    default,
                    TenantId),
                cancellationToken
            );

        public async Task DispatchSignalAsync(string signal, object? input = default, string? workflowInstanceId = default, CancellationToken cancellationToken = default) =>
            await _workflowDispatcher.DispatchAsync(new TriggerWorkflowsRequest(
                    nameof(SignalReceived),
                    new SignalReceivedBookmark {Signal = signal, WorkflowInstanceId = workflowInstanceId},
                    new SignalReceivedBookmark {Signal = signal},
                    new Signal(signal, input)),
                cancellationToken);
    }
}