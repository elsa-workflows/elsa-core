using Elsa.Models;

namespace Elsa.Events
{
    public class WorkflowDefinitionRetracted : WorkflowDefinitionNotification
    {
        public WorkflowDefinitionRetracted(WorkflowDefinition workflowDefinition) : base(workflowDefinition)
        {
        }
    }
}