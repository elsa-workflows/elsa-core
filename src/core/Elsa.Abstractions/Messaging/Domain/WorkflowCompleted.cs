using Elsa.Services.Models;

namespace Elsa.Messaging.Domain
{
    /// <summary>
    /// Published when a workflow transitioned into the Completed state.
    /// </summary>
    public class WorkflowCompleted : WorkflowNotification
    {
        public WorkflowCompleted(WorkflowExecutionContext workflowExecutionContext) : base(workflowExecutionContext)
        {
        }
    }
}