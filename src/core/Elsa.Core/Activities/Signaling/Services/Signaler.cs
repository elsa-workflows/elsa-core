using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Signaling.Models;
using Elsa.Dispatch;
using Elsa.Services;
using MediatR;

namespace Elsa.Activities.Signaling.Services
{
    public class Signaler : ISignaler
    {
        // TODO: Design multi-tenancy. 
        private const string? TenantId = default;

        private readonly IMediator _mediator;
        private readonly IWorkflowDispatcher _workflowDispatcher;
        private readonly ITokenService _tokenService;

        public Signaler(IMediator mediator, IWorkflowDispatcher workflowDispatcher, ITokenService tokenService)
        {
            _mediator = mediator;
            _workflowDispatcher = workflowDispatcher;
            _tokenService = tokenService;
        }

        public async Task<bool> TriggerSignalTokenAsync(string token, object? input = default, CancellationToken cancellationToken = default)
        {
            if (!_tokenService.TryDecryptToken(token, out SignalModel signal))
                return false;

            await TriggerSignalAsync(signal.Name, input, signal.WorkflowInstanceId, cancellationToken: cancellationToken);
            return true;
        }

        public async Task TriggerSignalAsync(string signal, object? input = default, string? workflowInstanceId = default, string? correlationId = default, CancellationToken cancellationToken = default)
        {
            await _mediator.Send(new TriggerWorkflowsRequest(
                nameof(SignalReceived),
                new SignalReceivedBookmark { Signal = signal, WorkflowInstanceId = workflowInstanceId },
                new SignalReceivedBookmark { Signal = signal },
                new Signal(signal, input),
                correlationId,
                workflowInstanceId,
                default,
                TenantId
            ), cancellationToken);
        }

        public async Task<bool> DispatchSignalTokenAsync(string token, object? input = default, CancellationToken cancellationToken = default)
        {
            if (!_tokenService.TryDecryptToken(token, out SignalModel signal))
                return false;

            await DispatchSignalAsync(signal.Name, input, signal.WorkflowInstanceId, cancellationToken: cancellationToken);
            return true;
        }

        public async Task DispatchSignalAsync(string signal, object? input = default, string? workflowInstanceId = default, string? correlationId = default, CancellationToken cancellationToken = default) =>
            await _workflowDispatcher.DispatchAsync(new TriggerWorkflowsRequest(
                    nameof(SignalReceived),
                    new SignalReceivedBookmark { Signal = signal, WorkflowInstanceId = workflowInstanceId },
                    new SignalReceivedBookmark { Signal = signal },
                    new Signal(signal, input),
                    correlationId,
                    workflowInstanceId
                ),
                cancellationToken);
    }
}