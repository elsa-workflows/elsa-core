using Elsa.Models;

namespace Elsa.Events
{
    public class WorkflowDefinitionDeleting : WorkflowDefinitionNotification
    {
        public WorkflowDefinitionDeleting(WorkflowDefinition workflowDefinition) : base(workflowDefinition)
        {
        }
    }
}