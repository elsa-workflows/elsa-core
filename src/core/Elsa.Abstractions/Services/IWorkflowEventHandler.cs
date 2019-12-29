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
        /// Invoked when an activity has executed.
        /// </summary>
        Task ActivityExecutedAsync(ProcessExecutionContext processExecutionContext, IActivity activity, CancellationToken cancellationToken);
        
        /// <summary>
        /// Invoked when an activity has faulted.
        /// </summary>
        Task ActivityFaultedAsync(ProcessExecutionContext processExecutionContext, IActivity activity, string message, CancellationToken cancellationToken);
        
        /// <summary>
        /// Invoked when halted activities are about to be executed.
        /// </summary>
        Task InvokingHaltedActivitiesAsync(ProcessExecutionContext processExecutionContext, CancellationToken cancellationToken);

        /// <summary>
        /// Invoked when workflow execution finished.
        /// </summary>
        Task WorkflowInvokedAsync(ProcessExecutionContext processExecutionContext, CancellationToken cancellationToken);
    }
}