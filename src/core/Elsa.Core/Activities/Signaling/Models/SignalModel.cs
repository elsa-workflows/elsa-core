namespace Elsa.Activities.Signaling.Models
{
    public class SignalModel
    {
        public SignalModel()
        {
        }

        public SignalModel(string name, string workflowInstanceId)
        {
            Name = name;
            WorkflowInstanceId = workflowInstanceId;
        }

        public string Name { get; set; } = default!;
        public string WorkflowInstanceId { get; set; } = default!;
    }
}