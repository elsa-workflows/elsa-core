using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Triggers
{
    public class WorkflowSelectorResult
    {
        public WorkflowSelectorResult(IWorkflowBlueprint workflowBlueprint, WorkflowInstance workflowInstance, string activityId, ITrigger trigger)
        {
            WorkflowBlueprint = workflowBlueprint;
            WorkflowInstance = workflowInstance;
            ActivityId = activityId;
            Trigger = trigger;
        }

        public IWorkflowBlueprint WorkflowBlueprint { get; }
        public WorkflowInstance WorkflowInstance { get; }
        public string ActivityId { get; }
        public ITrigger Trigger { get; }
    }
}