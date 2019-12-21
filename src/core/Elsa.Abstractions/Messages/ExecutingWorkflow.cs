using Elsa.Services.Models;

namespace Elsa.Messages
{
    public class ExecutingWorkflow : WorkflowNotification
    {
        public ExecutingWorkflow(Workflow workflow) : base(workflow)
        {
        }
    }
}