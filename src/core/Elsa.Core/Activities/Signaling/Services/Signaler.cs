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
        private readonly ITenantAccessor _tenantAccessor;

        public Signaler(IWorkflowLaunchpad workflowLaunchpad, ITokenService tokenService, ITenantAccessor tenantAccessor)
        {
            _workflowLaunchpad = workflowLaunchpad;
            _tokenService = tokenService;
            _tenantAccessor = tenantAccessor;
        }

        public async Task<IEnumerable<CollectedWorkflow>> TriggerSignalTokenAsync(string token, object? input = default, CancellationToken cancellationToken = default)
        {
            if (!_tokenService.TryDecryptToken(token, out SignalModel signal))
                return Enumerable.Empty<CollectedWorkflow>();

            return await TriggerSignalAsync(signal.Name, input, signal.WorkflowInstanceId, cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<CollectedWorkflow>> TriggerSignalAsync(string signal, object? input = default, string? workflowInstanceId = default, string? correlationId = default, CancellationToken cancellationToken = default)
        {
            var normalizedSignal = signal.ToLowerInvariant();

            var tenantId = await _tenantAccessor.GetTenantIdAsync(cancellationToken);

            return await _workflowLaunchpad.CollectAndExecuteWorkflowsAsync(new CollectWorkflowsContext(
                nameof(SignalReceived),
                new SignalReceivedBookmark { Signal = normalizedSignal, WorkflowInstanceId = workflowInstanceId },
                new SignalReceivedBookmark { Signal = normalizedSignal },
                correlationId,
                workflowInstanceId,
                default,
                tenantId
            ), new Signal(normalizedSignal, input), cancellationToken);
        }

        public async Task<IEnumerable<CollectedWorkflow>> DispatchSignalTokenAsync(string token, object? input = default, CancellationToken cancellationToken = default)
        {
            if (!_tokenService.TryDecryptToken(token, out SignalModel signal))
                return Enumerable.Empty<CollectedWorkflow>();

            return await DispatchSignalAsync(signal.Name, input, signal.WorkflowInstanceId, cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<CollectedWorkflow>> DispatchSignalAsync(string signal, object? input = default, string? workflowInstanceId = default, string? correlationId = default, CancellationToken cancellationToken = default)
        {
            var tenantId = await _tenantAccessor.GetTenantIdAsync(cancellationToken);

            return await _workflowLaunchpad.CollectAndDispatchWorkflowsAsync(new CollectWorkflowsContext(
                   nameof(SignalReceived),
                   new SignalReceivedBookmark { Signal = signal, WorkflowInstanceId = workflowInstanceId },
                   new SignalReceivedBookmark { Signal = signal },
                   correlationId,
                   workflowInstanceId,
                   default,
                   tenantId
               ),
               new Signal(signal, input),
               cancellationToken);
        }          
    }
}