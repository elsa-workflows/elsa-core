using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Timers.Services;
using Elsa.ActivityResults;
using Elsa.Models;
using Elsa.Repositories;
using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Activities.Timers.ActivityResults
{
    public class ScheduleWorkflowResult : ActivityExecutionResult
    {
        public ScheduleWorkflowResult(Instant executeAt)
        {
            ExecuteAt = executeAt;
        }

        public Instant ExecuteAt { get; }

        public override async ValueTask ExecuteAsync(ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var workflowInstance = activityExecutionContext.WorkflowExecutionContext.WorkflowInstance;
            var workflowInstanceRepository = activityExecutionContext.GetService<IWorkflowInstanceStore>();
            var scheduler = activityExecutionContext.GetService<IWorkflowScheduler>();
            await workflowInstanceRepository.SaveAsync(workflowInstance, cancellationToken);
            await scheduler.ScheduleWorkflowAsync(activityExecutionContext.WorkflowExecutionContext.WorkflowBlueprint, workflowInstance.WorkflowInstanceId, activityExecutionContext.ActivityInstance.Id, ExecuteAt, cancellationToken);
        }
    }
}