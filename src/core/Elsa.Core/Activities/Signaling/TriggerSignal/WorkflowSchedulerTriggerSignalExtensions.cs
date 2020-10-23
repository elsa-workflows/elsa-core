using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Signaling;
using Elsa.Services;

// ReSharper disable once CheckNamespace
namespace Elsa
{
    public static class WorkflowSchedulerTriggerSignalExtensions
    {
        public static async Task TriggerSignalAsync(
            this IWorkflowScheduler workflowRunner,
            string signalName,
            string? correlationId = default,
            CancellationToken cancellationToken = default) =>
            await workflowRunner.TriggerWorkflowsAsync(nameof(Signaled), signalName, correlationId, cancellationToken);
    }
}