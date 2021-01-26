namespace Elsa.Triggers
{
    public class WorkflowSelectorResult
    {
        public WorkflowSelectorResult(string workflowInstanceId, string activityId, ITrigger trigger)
        {
            WorkflowInstanceId = workflowInstanceId;
            ActivityId = activityId;
            Trigger = trigger;
        }

        public string WorkflowInstanceId { get; }
        public string ActivityId { get; }
        public ITrigger Trigger { get; }
    }
}