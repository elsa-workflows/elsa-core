using Elsa.Services.Models;

namespace Elsa.Messaging.Domain
{
    /// <summary>
    /// Published when a workflow transitioned into the Cancelled state.
    /// </summary>
    public class WorkflowCancelled : WorkflowNotification
    {
        public WorkflowCancelled(WorkflowExecutionContext workflowExecutionContext) : base(workflowExecutionContext)
        {
        }
    }
}