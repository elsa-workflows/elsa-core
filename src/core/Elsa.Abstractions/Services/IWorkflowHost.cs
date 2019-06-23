using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Runtime
{
    public interface IWorkflowHost
    {
        /// <summary>
        /// Starts new workflows that start with the specified activity name and resumes halted workflows that are blocked on activities with the specified activity name.
        /// </summary>
        Task TriggerWorkflowsAsync(string activityName, Variables arguments, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts a new instance of the specified workflow using the specified starting activity.
        /// </summary>
        Task<WorkflowExecutionContext> ExecuteWorkflowAsync(Workflow workflow, IActivity startActivity, CancellationToken cancellationToken = default);
    }
}