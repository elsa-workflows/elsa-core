using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using MediatR;

namespace Elsa.Handlers
{
    /// <summary>
    /// Reschedules the current parent activity in scope.
    /// </summary>
    public class RescheduleBranchingActivitiesAndContainers : INotificationHandler<WorkflowExecutionBurstCompleted>
    {
        public Task Handle(WorkflowExecutionBurstCompleted notification, CancellationToken cancellationToken)
        {
            var activityExecutionContext = notification.ActivityExecutionContext;
            var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;

            // Check to see if a suspension / completion has been instructed. If so, do nothing.
            if (workflowExecutionContext.HasScheduledActivities || workflowExecutionContext.Status != WorkflowStatus.Running)
                return Task.CompletedTask;
            
            // Check if we are within a scope.
            if(!workflowExecutionContext.WorkflowInstance.Scopes.Any())
                return Task.CompletedTask;

            // Re-schedule the current scope activity.
            var scope = workflowExecutionContext.WorkflowInstance.Scopes.Pop();
            workflowExecutionContext.WorkflowInstance.ActivityData.GetItem(scope.ActivityId)!.SetState("Unwinding", true);
            workflowExecutionContext.ScheduleActivity(scope.ActivityId);
            
            return Task.CompletedTask;
        }
    }
}