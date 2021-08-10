using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Services.Models;
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
            var workflowInstance = workflowExecutionContext.WorkflowInstance;
            var scope = GetScope(activityExecutionContext);

            // Check to see if a suspension / completion has been instructed.
            if (workflowExecutionContext.HasScheduledActivities || workflowExecutionContext.Status != WorkflowStatus.Running)
            {
                // If any blocking activity is within the current scope, exit.
                var anyInScope = scope == null || IsAnyBlockingActivityInScope(workflowExecutionContext, scope.ActivityId);
                
                if(anyInScope || workflowExecutionContext.Status == WorkflowStatus.Finished)
                    return Task.CompletedTask;
            }
            
            if (scope == null)
            {
                // No scope to reschedule.
                return Task.CompletedTask;
            }

            // Re-schedule the current scope activity.
            workflowInstance.Scopes.Remove(scope);
            workflowInstance.ActivityData.GetItem(scope.ActivityId)!.SetState("Unwinding", true);
            workflowExecutionContext.ScheduleActivity(scope.ActivityId);

            return Task.CompletedTask;
        }

        private bool IsAnyBlockingActivityInScope(WorkflowExecutionContext workflowExecutionContext, string scopeActivityId)
        {
            // Get all blocking activity IDs.
            var blockingActivityIds = workflowExecutionContext.WorkflowInstance.BlockingActivities.Select(x => x.ActivityId).ToHashSet();

            // For each blocking activity, check if it is within the current scope (taking the first one).
            foreach (var blockingActivityId in blockingActivityIds)
            {
                var inboundActivityIds = new[] { blockingActivityId }.Concat(workflowExecutionContext.GetInboundActivityPath(blockingActivityId)).ToHashSet();
                if (inboundActivityIds.Contains(scopeActivityId))
                    return true;
            }

            return false;
        }

        private ActivityScope? GetScope(ActivityExecutionContext context)
        {
            var workflowExecutionContext = context.WorkflowExecutionContext;
            
            // Check if we are within a scope.
            if (!workflowExecutionContext.WorkflowInstance.Scopes.Any())
                return null;

            // Select the scope that is within the activity's inbound trajectory. Include current activity for consideration (it might itself be a scope).
            var activityId = context.ActivityId;
            var inboundActivityIds = new[] { activityId }.Concat(workflowExecutionContext.GetInboundActivityPath(activityId)).ToHashSet();
            var workflowInstance = workflowExecutionContext.WorkflowInstance;
            var scopes = workflowInstance.Scopes;
            var scope = scopes.FirstOrDefault(x => inboundActivityIds.Contains(x.ActivityId));

            return scope;
        }
    }
}