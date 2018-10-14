using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.ActivityResults;
using Flowsharp.Models;

namespace Flowsharp.Activities
{
    public interface IActivity
    {
        /// <summary>
        /// Returns a value of whether the specified activity can execute.
        /// </summary>
        Task<bool> CanExecuteAsync(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext, CancellationToken cancellationToken);

        /// <summary>
        /// Executes the specified activity.
        /// </summary>
        Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext, CancellationToken cancellationToken);

        /// <summary>
        /// Resumes the specified activity.
        /// </summary>
        Task<ActivityExecutionResult> ResumeAsync(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext, CancellationToken cancellationToken);

        /// <summary>
        /// Executes before a workflow starts or resumes, giving activities an opportunity to read and store any values of interest.
        /// </summary>
        Task ReceiveInputAsync(WorkflowExecutionContext workflowContext, IDictionary<string, object> input, CancellationToken cancellationToken);

        /// <summary>
        /// Executes when a workflow is about to start.
        /// </summary>
        Task WorkflowStartingAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken);

        /// <summary>
        /// Executes when a workflow has started.
        /// </summary>
        Task WorkflowStartedAsync(WorkflowExecutionContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Executes when a workflow is about to be resumed.
        /// </summary>
        Task WorkflowResumingAsync(WorkflowExecutionContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Executes when a workflow is resumed.
        /// </summary>
        Task WorkflowResumedAsync(WorkflowExecutionContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Executes when an activity is about to be executed.
        /// </summary>
        /// <param name="workflowContext">The workflow execution context.</param>
        /// <param name="activityContext">The activity context containing the activity that is the subject of the event.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task OnActivityExecutingAsync(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Called on each activity when an activity has been executed.
        /// </summary>
        /// <param name="workflowContext">The workflow execution context.</param>
        /// <param name="activityContext">The activity context containing the activity that is the subject of the event.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task OnActivityExecutedAsync(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext, CancellationToken cancellationToken);
    }
}
