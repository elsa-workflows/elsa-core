using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Events;
using MediatR;

namespace Elsa.Handlers
{
    public class ConnectNextActivityViaDefaultOutcome : INotificationHandler<WorkflowExecutionBurstCompleted>
    {
        public Task Handle(WorkflowExecutionBurstCompleted notification, CancellationToken cancellationToken)
        {
            var activityExecutionContext = notification.ActivityExecutionContext;
            var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;

            // Check to see if there are connections with the last-executed activity's "Done" outcome.
            // This handles the case for workflows built using the WorkflowBuilder API where activities are chained, expecting the chained activities to execute next, regardless of the activity's outcome.

            var nextActivities = OutcomeResult.GetNextActivities(workflowExecutionContext, activityExecutionContext.ActivityId, new[] {OutcomeNames.Done}).ToList();
            workflowExecutionContext.ScheduleActivities(nextActivities, activityExecutionContext.Output);
            
            return Task.CompletedTask;
        }
    }
}