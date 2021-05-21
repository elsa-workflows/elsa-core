using Elsa.Models;

namespace Elsa.Events
{
    public class WorkflowDefinitionRetracting : WorkflowDefinitionNotification
    {
        public WorkflowDefinitionRetracting(WorkflowDefinition workflowDefinition) : base(workflowDefinition)
        {
        }
    }
}