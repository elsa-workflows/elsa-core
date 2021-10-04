using System;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{
    public class ActivityExecutionResultFailed : INotification
    {
        public ActivityExecutionResultFailed(Exception exception, ActivityExecutionContext activityExecutionContext)
        {
            Exception = exception;
            ActivityExecutionContext = activityExecutionContext;
        }

        public Exception Exception { get; }
        public ActivityExecutionContext ActivityExecutionContext { get; }
    }
}