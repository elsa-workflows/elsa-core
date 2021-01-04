using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{
    /// <summary>
    /// Fired when an activity has executed and a pass was completed.
    /// </summary>
    public class WorkflowExecutionPassCompleted : INotification
    {
        public WorkflowExecutionPassCompleted(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext)
        {
            WorkflowExecutionContext = workflowExecutionContext;
            ActivityExecutionContext = activityExecutionContext;
        }
        
        public WorkflowExecutionContext WorkflowExecutionContext { get; }
        public ActivityExecutionContext ActivityExecutionContext { get; }
    }
}