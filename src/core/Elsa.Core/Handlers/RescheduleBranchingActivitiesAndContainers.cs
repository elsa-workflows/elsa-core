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

            // If no suspension has been instructed, re-schedule any container activities.
            if (workflowExecutionContext.HasScheduledActivities || workflowExecutionContext.Status != WorkflowStatus.Running || !workflowExecutionContext.WorkflowInstance.Scopes.Any())
                return Task.CompletedTask;

            var parentActivityId = workflowExecutionContext.WorkflowInstance.Scopes.Pop();
            workflowExecutionContext.ScheduleActivity(parentActivityId, activityExecutionContext.Output);
            
            return Task.CompletedTask;
        }
    }
}