using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Services
{
    public static class WorkflowHostExtensions
    {
        public static Task<WorkflowExecutionContext> RunAsync<T>(this IWorkflowHost workflowHost, object? input = default, CancellationToken cancellationToken = default) 
            => workflowHost.RunAsync(typeof(T).Name, input, cancellationToken);

        /// <summary>
        /// Starts new workflows that start with the specified activity name and resumes halted workflows that are blocked on activities with the specified activity name.
        /// </summary>
        public static Task TriggerAsync(
            this IWorkflowHost workflowHost,
            string activityType,
            Variable input,
            CancellationToken cancellationToken = default)
        {
            return workflowHost.TriggerAsync(activityType, input, cancellationToken: cancellationToken);
        }
    }
}