using Elsa.Services.Models;

namespace Elsa.Triggers
{
    public class WorkflowSelectorResult
    {
        public WorkflowSelectorResult(IWorkflowBlueprint workflowBlueprint, string activityId)
        {
            WorkflowBlueprint = workflowBlueprint;
            ActivityId = activityId;
        }

        public IWorkflowBlueprint WorkflowBlueprint { get; }
        public string ActivityId { get; }
    }
}