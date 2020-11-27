using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Timers.Jobs;
using Elsa.Services.Models;
using NodaTime;
using Quartz;

namespace Elsa.Activities.Timers.Services
{
    public class WorkflowScheduler : IWorkflowScheduler
    {
        private readonly ISchedulerFactory _schedulerFactory;

        public WorkflowScheduler(ISchedulerFactory schedulerFactory)
        {
            _schedulerFactory = schedulerFactory;
        }
        
        public async Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string activityId, Instant startAt, Duration interval, CancellationToken cancellationToken = default)
        {
            var workflow = workflowBlueprint;
            var groupName = $"tenant:{workflow.TenantId}-workflow:{workflow.Id}";

            var job = JobBuilder.Create<RunWorkflowJob>()
                .WithIdentity($"activity:{activityId}", groupName)
                .UsingJobData("TenantId", workflow.TenantId!)
                .UsingJobData("WorkflowDefinitionId", workflow.Id)
                .UsingJobData("ActivityId", activityId)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"activity:{activityId}", groupName)
                .StartAt(startAt.ToDateTimeOffset())
                .WithSimpleSchedule(x => x.WithInterval(interval.ToTimeSpan()).RepeatForever())
                .Build();

            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            await scheduler.ScheduleJob(job, trigger, cancellationToken);
        }
        
        public async Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string activityId, Instant startAt, CancellationToken cancellationToken = default)
        {
            var workflow = workflowBlueprint;
            var groupName = $"tenant:{workflow.TenantId}-workflow:{workflow.Id}";

            var job = JobBuilder.Create<RunWorkflowJob>()
                .WithIdentity($"activity:{activityId}", groupName)
                .UsingJobData("TenantId", workflow.TenantId!)
                .UsingJobData("WorkflowDefinitionId", workflow.Id)
                .UsingJobData("ActivityId", activityId)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"activity:{activityId}", groupName)
                .StartAt(startAt.ToDateTimeOffset())
                .Build();

            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            await scheduler.ScheduleJob(job, trigger, cancellationToken);
        }
        
        public async Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string activityId, string cronExpression, CancellationToken cancellationToken = default)
        {
            var workflow = workflowBlueprint;
            var groupName = $"tenant:{workflow.TenantId}-workflow:{workflow.Id}";

            var job = JobBuilder.Create<RunWorkflowJob>()
                .WithIdentity($"activity:{activityId}", groupName)
                .UsingJobData("TenantId", workflow.TenantId!)
                .UsingJobData("WorkflowDefinitionId", workflow.Id)
                .UsingJobData("ActivityId", activityId)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"activity:{activityId}", groupName)
                .WithCronSchedule(cronExpression)
                .Build();

            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            await scheduler.ScheduleJob(job, trigger, cancellationToken);
        }

        public async Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string workflowInstanceId, string activityId, Instant startAt, CancellationToken cancellationToken = default)
        {
            var workflow = workflowBlueprint;
            var groupName = $"tenant:{workflow.TenantId}-workflow-instance:{workflowInstanceId}";

            var job = JobBuilder.Create<RunWorkflowJob>()
                .WithIdentity($"activity:{activityId}", groupName)
                .UsingJobData("TenantId", workflow.TenantId!)
                .UsingJobData("WorkflowDefinitionId", workflow.Id)
                .UsingJobData("WorkflowInstanceId", workflowInstanceId)
                .UsingJobData("ActivityId", activityId)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"activity:{activityId}", groupName)
                .StartAt(startAt.ToDateTimeOffset())
                .Build();

            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            await scheduler.ScheduleJob(job, trigger, cancellationToken);
        }
    }
}