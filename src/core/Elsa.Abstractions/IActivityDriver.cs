using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Results;

namespace Elsa
{
    public interface IActivityDriver
    {
        /// <summary>
        /// The activity type of this driver.
        /// </summary>
        string ActivityType { get; }
        
        /// <summary>
        /// Returns a value of whether the specified activity can execute.
        /// </summary>
        Task<bool> CanExecuteAsync(ActivityExecutionContext activityContext, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken);

        /// <summary>
        /// Executes the specified activity.
        /// </summary>
        Task<ActivityExecutionResult> ExecuteAsync(ActivityExecutionContext activityContext, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken);

        /// <summary>
        /// Resumes the specified activity.
        /// </summary>
        Task<ActivityExecutionResult> ResumeAsync(ActivityExecutionContext activityContext, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken);
    }
}