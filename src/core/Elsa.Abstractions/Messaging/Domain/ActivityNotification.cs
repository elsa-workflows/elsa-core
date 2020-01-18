﻿using Elsa.Services;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Messaging.Domain
{
    public abstract class ActivityNotification : INotification
    {
        protected ActivityNotification(ActivityExecutionContext activityExecutionContext)
        {
            ActivityExecutionContext = activityExecutionContext;
        }

        public ActivityExecutionContext ActivityExecutionContext { get; }
        public WorkflowExecutionContext WorkflowExecutionContext => ActivityExecutionContext.WorkflowExecutionContext;
        public IActivity Activity => ActivityExecutionContext.Activity;
    }
}