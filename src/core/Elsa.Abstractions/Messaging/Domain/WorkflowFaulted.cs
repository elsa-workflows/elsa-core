using Elsa.Services.Models;

namespace Elsa.Messaging.Domain
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