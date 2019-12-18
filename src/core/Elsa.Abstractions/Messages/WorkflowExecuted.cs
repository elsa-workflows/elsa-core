using Elsa.Services.Models;

namespace Elsa.Messages
{
    /// <summary>
    /// Published when a burst of execution finished.
    /// </summary>
    public class WorkflowExecuted  : WorkflowNotification
    {
        public WorkflowExecuted(Workflow workflow) : base(workflow)
        {
        }
    }
}