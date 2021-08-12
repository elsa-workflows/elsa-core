using Elsa.Services.Models;

namespace Elsa.Events
{
    public class WorkflowBlueprintLoaded : WorkflowBlueprintNotification
    {
        public WorkflowBlueprintLoaded(WorkflowBlueprint workflowBlueprint) : base(workflowBlueprint)
        {
        }
    }
}
