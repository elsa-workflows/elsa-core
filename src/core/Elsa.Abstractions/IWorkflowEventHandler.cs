using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa
{
    /// <summary>
    /// Implement this in order to receive various events related to workflow execution
    /// </summary>
    public interface IWorkflowEventHandler
    {

        /// <summary>
        /// Invoked when an activity has executed.
        /// </summary>
        Task ActivityExecutedAsync(WorkflowExecutionContext workflowExecutionContext, IActivity activity, CancellationToken cancellationToken);
        
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