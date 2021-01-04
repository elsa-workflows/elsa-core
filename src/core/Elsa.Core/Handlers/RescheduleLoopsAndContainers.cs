using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.ControlFlow;
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
    public class RescheduleLoopsAndContainers : INotificationHandler<WorkflowExecutionBurstCompleted>
    {
        public Task Handle(WorkflowExecutionBurstCompleted notification, CancellationToken cancellationToken)
        {
            ScheduleLoops(notification);
            ScheduleContainers(notification);
            return Task.CompletedTask;
        }

        private static void ScheduleLoops(WorkflowExecutionBurstCompleted notification)
        {
            var workflowExecutionContext = notification.WorkflowExecutionContext;

            // If no suspension has been instructed, re-schedule any post-scheduled activities.
            if (workflowExecutionContext.HasScheduledActivities || workflowExecutionContext.Status != WorkflowStatus.Running)
                return;

            var workflowBlueprint = workflowExecutionContext.WorkflowBlueprint;
            var currentActivityId = notification.ActivityExecutionContext.ActivityBlueprint.Id;
            var inboundConnections = workflowBlueprint.GetInboundConnectionPath(currentActivityId).ToList();

            var query = 
                from inboundConnection in inboundConnections 
                let parentActivityBlueprint = inboundConnection.Source.Activity 
                where inboundConnection.Source.Outcome == OutcomeNames.Iterate 
                select parentActivityBlueprint;

            var firstLoop = query.FirstOrDefault();
            
            if(firstLoop != null)
                workflowExecutionContext.ScheduleActivity(firstLoop.Id);
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