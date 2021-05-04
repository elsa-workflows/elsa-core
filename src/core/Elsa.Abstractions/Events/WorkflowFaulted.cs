using Elsa.Services.Models;

namespace Elsa.Events
{
    /// <summary>
    /// Published when a workflow transitioned into the Faulted state.
    /// </summary>
    public class WorkflowFaulted : WorkflowNotification
    {
        public WorkflowFaulted(WorkflowExecutionContext workflowExecutionContext) : base(workflowExecutionContext)
        {
        }
    }
}