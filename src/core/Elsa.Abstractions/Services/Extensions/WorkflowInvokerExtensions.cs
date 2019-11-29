using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services.Extensions
{
    public static class WorkflowInvokerExtensions
    {
        /// <summary>
        /// Starts new workflows that start with the specified activity name and resumes halted workflows that are blocked on activities with the specified activity name.
        /// </summary>
        public static Task TriggerAsync(
            this IWorkflowRunner workflowRunner,
            string activityType,
            Variables input,
            CancellationToken cancellationToken = default)
        {
            return workflowRunner.TriggerAsync(activityType, input, cancellationToken: cancellationToken);
        }
    }
}