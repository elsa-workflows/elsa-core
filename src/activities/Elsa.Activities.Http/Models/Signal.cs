namespace Elsa.Activities.Http.Models
{
    public class Signal
    {
        public Signal()
        {
        }

        public Signal(string name, string workflowInstanceId, string activityId)
        {
            Name = name;
            WorkflowInstanceId = workflowInstanceId;
            ActivityId = activityId;
        }

        public string Name { get; set; }
        public string WorkflowInstanceId { get; set; }
        public string ActivityId { get; set; }
    }
}