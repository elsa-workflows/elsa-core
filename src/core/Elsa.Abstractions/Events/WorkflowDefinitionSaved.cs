using Elsa.Models;

namespace Elsa.Events
{
    public class WorkflowDefinitionSaved : WorkflowDefinitionNotification
    {
        public WorkflowDefinitionSaved(WorkflowDefinition workflowDefinition) : base(workflowDefinition)
        {
        }
    }
}