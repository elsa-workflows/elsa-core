using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using MediatR;

namespace Elsa.Handlers
{
    /// <summary>
    /// Walks up the tree of inbound connections along the "Iterate" outcome of the looping construct (While/For/ForEach) and re-schedules the looping activity.
    /// Also handles composite activity re-scheduling.
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