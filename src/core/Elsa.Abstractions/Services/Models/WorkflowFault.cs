namespace Elsa.Services.Models
{
    public class WorkflowFault
    {
        public IActivity FaultedActivity { get; set; }
        public string Message { get; set; }
    }
}