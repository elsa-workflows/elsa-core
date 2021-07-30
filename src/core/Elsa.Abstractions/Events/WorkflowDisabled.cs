using Elsa.Services.Models;

namespace Elsa.Events
{
    /// <summary>
    /// Published when a workflow transitioned into the Disabled state.
    /// </summary>
    public class WorkflowDisabled : WorkflowNotification
    {
        public WorkflowDisabled(WorkflowExecutionContext workflowExecutionContext) : base(workflowExecutionContext)
        {
        }
    }
}