namespace Elsa.Messaging.Distributed
{
    public class RunWorkflow
    {
        public RunWorkflow(string workflowInstanceId, string? activityId = default, object? input = default)
        {
            WorkflowInstanceId = workflowInstanceId;
            ActivityId = activityId;
            Input = input;
        }

        public string WorkflowInstanceId { get; set; }
        public string? ActivityId { get; set; }
        public object? Input { get; set; }
    }
}