using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Signaling.Models;
using Elsa.Services;

namespace Elsa.Activities.Signaling.Services
{
    public class Signaler : ISignaler
    {
        private readonly IWorkflowRunner _workflowScheduler;

        public Signaler(IWorkflowRunner workflowScheduler)
        {
            _workflowScheduler = workflowScheduler;
        }

        public async Task SendSignalAsync(string signal, object? input = default, string? correlationId = default, CancellationToken cancellationToken = default) =>
            await _workflowScheduler.TriggerWorkflowsAsync<SignalReceivedTrigger>(
                x => x.Signal == signal && (x.CorrelationId == null || x.CorrelationId == correlationId),
                new Signal(signal, input),
                correlationId,
                cancellationToken: cancellationToken
            );
    }
}