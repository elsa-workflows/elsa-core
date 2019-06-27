namespace Elsa.Services.Models
{
    public class WorkflowFault
    {
        public IActivity FaultedActivity { get; set; }
        public string Message { get; set; }

        public Serialization.Models.WorkflowFault ToInstance()
        {
            return new Serialization.Models.WorkflowFault
            {
                FaultedActivityId = FaultedActivity?.Id,
                Message = Message
            };
        }
    }
}