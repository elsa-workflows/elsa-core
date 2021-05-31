using Elsa.Models;

namespace Elsa.Events
{
    public class WorkflowDefinitionPublishing : WorkflowDefinitionNotification
    {
        public WorkflowDefinitionPublishing(WorkflowDefinition workflowDefinition) : base(workflowDefinition)
        {
        }
    }
}