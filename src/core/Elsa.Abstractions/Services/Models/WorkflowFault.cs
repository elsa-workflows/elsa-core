using System;

namespace Elsa.Services.Models
{
    public class WorkflowFault
    {
        public IActivity FaultedActivity { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }

        public Elsa.Models.WorkflowFault ToInstance()
        {
            return new Elsa.Models.WorkflowFault
            {
                FaultedActivityId = FaultedActivity?.Id,
                Message = Message,
                Exception = Exception
            };
        }
    }
}