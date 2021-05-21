using Elsa.Models;

namespace Elsa.Events
{
    public class WorkflowDefinitionPublished : WorkflowDefinitionNotification
    {
        public WorkflowDefinitionPublished(WorkflowDefinition workflowDefinition) : base(workflowDefinition)
        {
        }
    }
}