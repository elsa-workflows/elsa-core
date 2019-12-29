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
            this IProcessRunner processRunner,
            string activityType,
            Variable input,
            CancellationToken cancellationToken = default)
        {
            return processRunner.TriggerAsync(activityType, input, cancellationToken: cancellationToken);
        }
    }
}