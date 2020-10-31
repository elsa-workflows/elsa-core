using Elsa.Services.Models;

namespace Elsa.Triggers
{
    public class WorkflowSelectorResult
    {
        public WorkflowSelectorResult(IWorkflowBlueprint workflowBlueprint, string? workflowInstanceId, string activityId, ITrigger trigger)
        {
            WorkflowBlueprint = workflowBlueprint;
            WorkflowInstanceId = workflowInstanceId;
            ActivityId = activityId;
            Trigger = trigger;
        }

        public IWorkflowBlueprint WorkflowBlueprint { get; }
        public string? WorkflowInstanceId { get; }
        public string ActivityId { get; }
        public ITrigger Trigger { get; }
    }
}