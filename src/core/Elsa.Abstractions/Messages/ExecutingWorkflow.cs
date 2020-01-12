using Elsa.Services.Models;

namespace Elsa.Messages
{
    public class ExecutingWorkflow : WorkflowNotification
    {
        public ExecutingWorkflow(WorkflowExecutionContext workflowExecutionContext) : base(workflowExecutionContext)
        {
        }
    }
}