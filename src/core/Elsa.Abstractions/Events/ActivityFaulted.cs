using System;
using Elsa.Services.Models;

namespace Elsa.Events
{
    public class ActivityFaulted : ActivityNotification
    {
        public ActivityFaulted(Exception exception, ActivityExecutionContext activityExecutionContext) : base(activityExecutionContext)
        {
            Exception = exception;
        }
        
        public Exception Exception { get; }
    }
}