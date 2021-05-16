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

        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        private readonly ITokenService _tokenService;

        public Signaler(IWorkflowLaunchpad workflowLaunchpad, ITokenService tokenService)
        {
            _workflowLaunchpad = workflowLaunchpad;
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
            await _workflowLaunchpad.TriggerWorkflowsAsync(new CollectWorkflowsContext(
                nameof(SignalReceived),
                new SignalReceivedBookmark { Signal = signal, WorkflowInstanceId = workflowInstanceId },
                new SignalReceivedBookmark { Signal = signal },
                correlationId,
                workflowInstanceId,
                default,
                TenantId
            ), new Signal(signal, input), cancellationToken);
        }

        public async Task<bool> DispatchSignalTokenAsync(string token, object? input = default, CancellationToken cancellationToken = default)
        {
            if (!_tokenService.TryDecryptToken(token, out SignalModel signal))
                return false;

            await DispatchSignalAsync(signal.Name, input, signal.WorkflowInstanceId, cancellationToken: cancellationToken);
            return true;
        }

        public async Task DispatchSignalAsync(string signal, object? input = default, string? workflowInstanceId = default, string? correlationId = default, CancellationToken cancellationToken = default) =>
            await _workflowLaunchpad.DispatchWorkflowsAsync(new CollectWorkflowsContext(
                    nameof(SignalReceived),
                    new SignalReceivedBookmark { Signal = signal, WorkflowInstanceId = workflowInstanceId },
                    new SignalReceivedBookmark { Signal = signal },
                    correlationId,
                    workflowInstanceId,
                    default,
                    TenantId
                ),
                new Signal(signal, input),
                cancellationToken);
    }
}