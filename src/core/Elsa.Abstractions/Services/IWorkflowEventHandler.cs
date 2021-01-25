using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    /// <summary>
    /// Implement this in order to receive various events related to workflow execution
    /// </summary>
    public interface IWorkflowEventHandler
    {
        /// <summary>
        /// Invoked before an activity execution.
        /// </summary>
        Task ExecutingActivityAsync(WorkflowExecutionContext workflowExecutionContext, IActivity activity, CancellationToken cancellationToken);

        /// <summary>
        /// Invoked when an activity has executed.
        /// </summary>
        Task ActivityExecutedAsync(WorkflowExecutionContext workflowExecutionContext, IActivity activity, CancellationToken cancellationToken);
        
        /// <summary>
        /// Invoked when an activity has faulted.
        /// </summary>
        Task ActivityFaultedAsync(WorkflowExecutionContext workflowExecutionContext, IActivity activity, string message, CancellationToken cancellationToken);
        
        /// <summary>
        /// Invoked when halted activities are about to be executed.
        /// </summary>
        Task InvokingHaltedActivitiesAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken);

        /// <summary>
        /// Invoked when workflow execution finished.
        /// </summary>
        Task WorkflowInvokedAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken);
    }
}