using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Runtime
{
    public interface IWorkflowHost
    {
        /// <summary>
        /// Starts new workflows that start with the specified activity name and resumes halted workflows that are blocked on activities with the specified activity name.
        /// </summary>
        Task TriggerWorkflowAsync(string activityName, Variables arguments, CancellationToken cancellationToken);

        /// <summary>
        /// Starts a new instance of the specified workflow using the specified starting activity.
        /// </summary>
        Task<WorkflowExecutionContext> StartWorkflowAsync(Workflow workflow, IActivity startActivity, Variables arguments, CancellationToken cancellationToken);
        
        /// <summary>
        /// Resumes the specified workflow instance using th specified blocking activity.
        /// </summary>
        Task<WorkflowExecutionContext> ResumeWorkflowAsync(Workflow workflow, IActivity activity, Variables arguments, CancellationToken cancellationToken);
    }
}