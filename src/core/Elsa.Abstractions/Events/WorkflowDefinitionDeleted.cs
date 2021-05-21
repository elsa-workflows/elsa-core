using Elsa.Models;

namespace Elsa.Events
{
    public class WorkflowDefinitionDeleted : WorkflowDefinitionNotification
    {
        public WorkflowDefinitionDeleted(WorkflowDefinition workflowDefinition) : base(workflowDefinition)
        {
        }
    }
}