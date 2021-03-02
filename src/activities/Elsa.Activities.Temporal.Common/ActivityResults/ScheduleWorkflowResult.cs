using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Services;
using Elsa.ActivityResults;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace Elsa.Activities.Temporal.ActivityResults
{
    public class ScheduleWorkflowResult : ActivityExecutionResult
    {
        public ScheduleWorkflowResult(Instant executeAt)
        {
            ExecuteAt = executeAt;
        }

        public Instant ExecuteAt { get; }

        protected override void Execute(ActivityExecutionContext activityExecutionContext)
        {
            var workflowInstanceId = activityExecutionContext.WorkflowExecutionContext.WorkflowInstance.Id;
            var activityId = activityExecutionContext.ActivityBlueprint.Id;
            var tenantId  = activityExecutionContext.WorkflowExecutionContext.WorkflowBlueprint.TenantId;
            var executeAt = ExecuteAt;
            
            async ValueTask ScheduleWorkflowAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
            {
                var scheduler = workflowExecutionContext.ServiceProvider.GetRequiredService<IWorkflowScheduler>(); 
                await scheduler.ScheduleWorkflowAsync(null, workflowInstanceId, activityId, tenantId, executeAt, null, cancellationToken);
            }

            activityExecutionContext.WorkflowExecutionContext.RegisterTask(nameof(ScheduleWorkflowResult), ScheduleWorkflowAsync);
        }
    }
}