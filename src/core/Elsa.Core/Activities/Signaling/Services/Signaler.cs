using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Signaling.Models;
using Elsa.Services;

namespace Elsa.Activities.Signaling.Services
{
    public class Signaler : ISignaler
    {
        // TODO: Figure out how to start jobs across multiple tenants / how to get a list of all tenants. 
        private const string TenantId = default;
        
        private readonly IWorkflowRunner _workflowScheduler;

        public Signaler(IWorkflowRunner workflowScheduler)
        {
            _workflowScheduler = workflowScheduler;
        }

        public async Task SendSignalAsync(string signal, object? input = default, string? correlationId = default, CancellationToken cancellationToken = default) =>
            await _workflowScheduler.TriggerWorkflowsAsync<SignalReceived>(
                new SignalReceivedBookmark{ Signal = signal, CorrelationId = correlationId},
                TenantId,
                new Signal(signal, input),
                correlationId,
                cancellationToken: cancellationToken
            );
    }
}