using System;

namespace Elsa.Models
{
    public class WorkflowFault
    {
        public string FaultedActivityId { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }
}