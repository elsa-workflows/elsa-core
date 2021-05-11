using Elsa.Models;

namespace Elsa.Events
{
    public class WorkflowDefinitionSaving : WorkflowDefinitionNotification
    {
        public WorkflowDefinitionSaving(WorkflowDefinition workflowDefinition) : base(workflowDefinition)
        {
        }
    }
}