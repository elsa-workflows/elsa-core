using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Elsa.Services
{
    public static class WorkflowSchedulerExtensions
    {
        /// <summary>
        /// Schedules a new workflow for execution using the workflow definition type.
        /// </summary>
        public static Task RunAsync<T>(this IWorkflowScheduler workflowScheduler, object? input = default, string? correlationId = default, CancellationToken cancellationToken = default) 
            => workflowScheduler.ScheduleNewWorkflowAsync(typeof(T).Name, input, correlationId, cancellationToken);
    }
}