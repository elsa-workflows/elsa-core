using Elsa.Services.Models;

namespace Elsa.Events
{
    /// <summary>
    /// Published when a workflow is about to be executed.
    /// </summary>
    public class WorkflowExecuting : WorkflowNotification
    {
        public WorkflowExecuting(WorkflowExecutionContext workflowExecutionContext) : base(workflowExecutionContext)
        {
        }
    }
}