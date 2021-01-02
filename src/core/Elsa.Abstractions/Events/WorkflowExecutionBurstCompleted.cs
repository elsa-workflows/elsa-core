using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{
    /// <summary>
    /// Fired when the workflow runner queue was completely processed.
    /// </summary>
    public class WorkflowExecutionBurstCompleted : INotification
    {
        public WorkflowExecutionBurstCompleted(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext)
        {
            WorkflowExecutionContext = workflowExecutionContext;
            ActivityExecutionContext = activityExecutionContext;
        }
        
        public WorkflowExecutionContext WorkflowExecutionContext { get; }
        public ActivityExecutionContext ActivityExecutionContext { get; }
    }
}