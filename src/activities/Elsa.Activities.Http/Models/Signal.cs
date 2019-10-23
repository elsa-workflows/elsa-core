namespace Elsa.Activities.Http.Models
{
    public class Signal
    {
        public Signal()
        {
        }

        public Signal(string name, string workflowInstanceId)
        {
            Name = name;
            WorkflowInstanceId = workflowInstanceId;
        }

        public string Name { get; set; }
        public string WorkflowInstanceId { get; set; }
    }
}