﻿namespace Elsa.Services.Models
{
    public class WorkflowFault
    {
        public WorkflowFault(IActivity faultedActivity, string message)
        {
            FaultedActivity = faultedActivity;
            Message = message;
        }
        
        public IActivity FaultedActivity { get; }
        public string Message { get; }

        public Elsa.Models.WorkflowFault ToInstance()
        {
            return new Elsa.Models.WorkflowFault
            {
                FaultedActivityId = FaultedActivity?.Id,
                Message = Message
            };
        }
    }
}