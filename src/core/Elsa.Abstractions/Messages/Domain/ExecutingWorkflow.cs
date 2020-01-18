using Elsa.Services.Models;

namespace Elsa.Messages.Domain
{
    public class ExecutingWorkflow : WorkflowNotification
    {
        public ExecutingWorkflow(WorkflowExecutionContext workflowExecutionContext) : base(workflowExecutionContext)
        {
        }
    }
}