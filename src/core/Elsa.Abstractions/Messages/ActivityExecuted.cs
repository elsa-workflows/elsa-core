using Elsa.Services;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Messages
{
    public class ActivityExecuted : INotification
    {
        public ActivityExecuted(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext)
        {
            WorkflowExecutionContext = workflowExecutionContext;
            ActivityExecutionContext = activityExecutionContext;
        }

        public WorkflowExecutionContext WorkflowExecutionContext { get; }
        public ActivityExecutionContext ActivityExecutionContext { get; }
        public IActivity Activity => ActivityExecutionContext.Activity;
    }
}