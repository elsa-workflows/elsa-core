using System;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Events
{
    public class ActivityFaulted : ActivityNotification
    {
        public ActivityFaulted(Exception exception, ActivityExecutionContext activityExecutionContext, IActivity activity) : base(activityExecutionContext, activity)
        {
            Exception = exception;
        }
        
        public Exception Exception { get; }
    }
}