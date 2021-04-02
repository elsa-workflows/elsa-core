using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Signaling.Models;
using Elsa.Dispatch;
using Elsa.Services;

namespace Elsa.Activities.Signaling.Services
{
    public class Signaler : ISignaler
    {
        // TODO: Design multi-tenancy. 
        private const string TenantId = default;

        private readonly ITriggersWorkflows _triggersWorkflows;
        private readonly IWorkflowDispatcher _workflowDispatcher;

        public Signaler(ITriggersWorkflows triggersWorkflows, IWorkflowDispatcher workflowDispatcher)
        {
            _triggersWorkflows = triggersWorkflows;
            _workflowDispatcher = workflowDispatcher;
        }

        public async Task TriggerSignalAsync(string signal, object? input = default, string? workflowInstanceId = default, CancellationToken cancellationToken = default) =>
            await _triggersWorkflows.TriggerWorkflowsAsync(
                nameof(SignalReceived),
                new SignalReceivedBookmark { Signal = signal, WorkflowInstanceId = workflowInstanceId },
                new SignalReceivedBookmark { Signal = signal },
                default,
                new Signal(signal, input),
                tenantId: TenantId,
                cancellationToken: cancellationToken
            );

        public async Task DispatchSignalAsync(string signal, object? input = default, string? workflowInstanceId = default, CancellationToken cancellationToken = default) =>
            await _workflowDispatcher.DispatchAsync(new TriggerWorkflowsRequest(
                    nameof(SignalReceived),
                    new SignalReceivedBookmark { Signal = signal, WorkflowInstanceId = workflowInstanceId },
                    new SignalReceivedBookmark { Signal = signal },
                    new Signal(signal, input)),
                cancellationToken);
    }
}