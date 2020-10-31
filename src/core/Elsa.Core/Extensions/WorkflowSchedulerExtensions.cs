using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

// ReSharper disable once CheckNamespace
namespace Elsa
{
    public static class WorkflowSchedulerExtensions
    {
        public static async Task ScheduleNewWorkflow<T>(
            this IWorkflowScheduler scheduler,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default) =>
            await scheduler.ScheduleWorkflowDefinitionAsync(typeof(T).Name, input, correlationId, contextId, cancellationToken);
    }
}