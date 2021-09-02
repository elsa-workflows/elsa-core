using Elsa.Services.Models;

namespace Elsa.Events
{
    public class WorkflowBlueprintLoaded : WorkflowBlueprintNotification
    {
        public WorkflowBlueprintLoaded(IWorkflowBlueprint workflowBlueprint) : base(workflowBlueprint)
        {
        }
    }
}
