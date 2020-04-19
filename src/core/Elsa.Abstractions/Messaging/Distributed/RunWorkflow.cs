namespace Elsa.Messaging.Distributed
{
    public class RunWorkflow
    {
        public RunWorkflow()
        {
        }

        public RunWorkflow(string instanceId, string? activityId = default, object? input = default)
        {
            InstanceId = instanceId;
            ActivityId = activityId;
            Input = input;
        }
        
        public string InstanceId { get; set; }
        public string? ActivityId { get; set; }
        public object? Input { get; set; }
    }
}