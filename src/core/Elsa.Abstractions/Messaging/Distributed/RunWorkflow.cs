using Elsa.Models;

namespace Elsa.Messaging.Distributed
{
    public class RunWorkflow
    {
        public RunWorkflow()
        {
        }

        public RunWorkflow(string instanceId, string? activityId = default, Variable? input = default)
        {
            InstanceId = instanceId;
            ActivityId = activityId;
            Input = input;
        }
        
        public string InstanceId { get; set; }
        public string? ActivityId { get; set; }
        public Variable? Input { get; set; }
    }
}