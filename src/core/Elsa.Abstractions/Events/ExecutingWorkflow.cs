using Elsa.Services.Models;

namespace Elsa.Events
{
    public class ExecutingWorkflow : WorkflowNotification
    {
        public ExecutingWorkflow(WorkflowExecutionContext workflowExecutionContext) : base(workflowExecutionContext)
        {
        }
    }
}