using Elsa.Services.Models;

namespace Elsa.Events
{
    /// <summary>
    /// Published when a burst of execution finished.
    /// </summary>
    public class WorkflowExecuted : WorkflowNotification
    {
        public WorkflowExecuted(WorkflowExecutionContext workflowExecutionContext) : base(workflowExecutionContext)
        {
        }
    }
}