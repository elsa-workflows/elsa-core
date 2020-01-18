using Elsa.Services.Models;

namespace Elsa.Messaging.Domain
{
    /// <summary>
    /// Published when a workflow transitioned into the Suspended state.
    /// </summary>
    public class WorkflowSuspended : WorkflowNotification
    {
        public WorkflowSuspended(WorkflowExecutionContext workflowExecutionContext) : base(workflowExecutionContext)
        {
        }
    }
}