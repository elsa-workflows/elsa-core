using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Handlers
{
    /// <summary>
    /// Walks up the tee of inbound connections along the "Iterate" outcome of the looping construct (While/For/ForEach) and re-schedules the looping activity.
    /// Also handles composite activity re-scheduling.
    /// </summary>
    public class RescheduleBranchingActivitiesAndContainers : INotificationHandler<WorkflowExecutionBurstCompleted>
    {
        private readonly IEnumerable<IBranchingActivity> _branchingActivities;

        public RescheduleBranchingActivitiesAndContainers(IEnumerable<IBranchingActivity> branchingActivities)
        {
            _branchingActivities = branchingActivities;
        }
        
        public Task Handle(WorkflowExecutionBurstCompleted notification, CancellationToken cancellationToken)
        {
            var workflowExecutionContext = notification.WorkflowExecutionContext;
            var activityExecutionContext = notification.ActivityExecutionContext;
            
            foreach (var branchingActivity in _branchingActivities)
            {
                if (workflowExecutionContext.HasScheduledActivities || workflowExecutionContext.Status != WorkflowStatus.Running)
                    break;
                
                branchingActivity.Unwind(activityExecutionContext);
            }
            
            ScheduleContainers(notification);
            return Task.CompletedTask;
        }

        private static void ScheduleContainers(WorkflowExecutionBurstCompleted notification)
        {
            var workflowExecutionContext = notification.WorkflowExecutionContext;
            var workflowBlueprint = workflowExecutionContext.WorkflowBlueprint;

            // If no suspension has been instructed, re-schedule any container activities.
            if (workflowExecutionContext.HasScheduledActivities || workflowExecutionContext.Status != WorkflowStatus.Running) 
                return;
            
            var activityBlueprint = notification.ActivityExecutionContext.ActivityBlueprint;
                
            // Re-schedule the parent activity, if any.
            if (activityBlueprint.Parent != null && workflowBlueprint.GetActivity(activityBlueprint.Parent.Id) != null)
            {
                var output = GetFinishOutput(workflowExecutionContext); 
                workflowExecutionContext.ScheduleActivity(activityBlueprint.Parent.Id, output);
            }
        }

        private static FinishOutput? GetFinishOutput(WorkflowExecutionContext workflowExecutionContext) => workflowExecutionContext.WorkflowInstance.Output as FinishOutput;
    }
}