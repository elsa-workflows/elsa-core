using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{
    /// <summary>
    /// Fired when the workflow runner is about to perform a burst of execution.
    /// </summary>
    public class WorkflowExecutionBurstStarting : INotification
    {
        public WorkflowExecutionBurstStarting(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext)
        {
            WorkflowExecutionContext = workflowExecutionContext;
            ActivityExecutionContext = activityExecutionContext;
        }
        
        public WorkflowExecutionContext WorkflowExecutionContext { get; }
        public ActivityExecutionContext ActivityExecutionContext { get; }
    }
}