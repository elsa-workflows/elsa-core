using Elsa.Services.Models;

namespace Elsa.Messaging.Domain
{
    public class ExecutingWorkflow : WorkflowNotification
    {
        public ExecutingWorkflow(WorkflowExecutionContext workflowExecutionContext) : base(workflowExecutionContext)
        {
        }
    }
}