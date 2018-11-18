namespace Flowsharp.Models
{
    public class WorkflowFault
    {
        public Flowsharp.IActivity FaultedActivity { get; set; }
        public string Message { get; set; }
    }
}