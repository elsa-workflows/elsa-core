using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Signaling.Models;
using Elsa.Services;
using Elsa.Services.Models;

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

        public async Task<IEnumerable<StartedWorkflow>> TriggerSignalTokenAsync(string token, object? input = default, CancellationToken cancellationToken = default)
        {
            if (!_tokenService.TryDecryptToken(token, out SignalModel signal))
                return Enumerable.Empty<StartedWorkflow>();

            return await TriggerSignalAsync(signal.Name, input, signal.WorkflowInstanceId, cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<StartedWorkflow>> TriggerSignalAsync(string signal, object? input = default, string? workflowInstanceId = default, string? correlationId = default, CancellationToken cancellationToken = default)
        {
            return await _workflowLaunchpad.CollectAndExecuteWorkflowsAsync(new CollectWorkflowsContext(
                nameof(SignalReceived),
                new SignalReceivedBookmark { Signal = signal, WorkflowInstanceId = workflowInstanceId },
                new SignalReceivedBookmark { Signal = signal },
                correlationId,
                workflowInstanceId,
                default,
                TenantId
            ), new Signal(signal, input), cancellationToken);
        }

        public async Task<IEnumerable<PendingWorkflow>> DispatchSignalTokenAsync(string token, object? input = default, CancellationToken cancellationToken = default)
        {
            if (!_tokenService.TryDecryptToken(token, out SignalModel signal))
                return Enumerable.Empty<PendingWorkflow>();

            return await DispatchSignalAsync(signal.Name, input, signal.WorkflowInstanceId, cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<PendingWorkflow>> DispatchSignalAsync(string signal, object? input = default, string? workflowInstanceId = default, string? correlationId = default, CancellationToken cancellationToken = default) =>
            await _workflowLaunchpad.CollectAndDispatchWorkflowsAsync(new CollectWorkflowsContext(
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