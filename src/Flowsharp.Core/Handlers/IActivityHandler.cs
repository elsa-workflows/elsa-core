using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;
using Flowsharp.ActivityResults;
using Flowsharp.Models;

namespace Flowsharp.Handlers
{
    public interface IActivityHandler
    {
    }
    
    public interface IActivityHandler<in T> : IActivityHandler where T:IActivity
    {
        /// <summary>
        /// Returns a value of whether the specified activity can execute.
        /// </summary>
        Task<bool> CanExecuteAsync(WorkflowExecutionContext workflowContext, T activity, CancellationToken cancellationToken);

        /// <summary>
        /// Executes the specified activity.
        /// </summary>
        Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, T activity, CancellationToken cancellationToken);

        /// <summary>
        /// Resumes the specified activity.
        /// </summary>
        Task<ActivityExecutionResult> ResumeAsync(WorkflowExecutionContext workflowContext, T activity, CancellationToken cancellationToken);

        /// <summary>
        /// Executes before a workflow starts or resumes, giving activities an opportunity to read and store any values of interest.
        /// </summary>
        Task ReceiveInputAsync(WorkflowExecutionContext workflowContext, Variables input, CancellationToken cancellationToken);

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
        Task OnActivityExecutingAsync(WorkflowExecutionContext workflowContext, T activity, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Called on each activity when an activity has been executed.
        /// </summary>
        Task OnActivityExecutedAsync(WorkflowExecutionContext workflowContext, T activity, CancellationToken cancellationToken);
    }
}