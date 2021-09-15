using Elsa.Models;

namespace Elsa.Events
{
    public class WorkflowDefinitionUpdated : WorkflowDefinitionNotification
    {
        public WorkflowDefinitionUpdated(WorkflowDefinition workflowDefinition) : base(workflowDefinition)
        {
        }
    }
}