using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Timers.Quartz.Jobs;
using Elsa.Activities.Timers.Services;
using Elsa.Services.Models;
using NodaTime;
using Quartz;

namespace Elsa.Activities.Timers.Quartz.Services
{
    public class QuartzWorkflowScheduler: IWorkflowScheduler
    {
        private static readonly string RunWorkflowJobKey = nameof(RunQuartzWorkflowJob);
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly SemaphoreSlim _semaphore = new(1);

        public QuartzWorkflowScheduler(ISchedulerFactory schedulerFactory)
        {
            _schedulerFactory = schedulerFactory;
        }

        public async Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string activityId, Instant startAt, Duration interval, CancellationToken cancellationToken = default)
        {
            var trigger = CreateTrigger(workflowBlueprint, activityId)
                .StartAt(startAt.ToDateTimeOffset())
                .WithSimpleSchedule(x => x.WithInterval(interval.ToTimeSpan()).RepeatForever())
                .Build();

            await ScheduleJob(trigger, cancellationToken);
        }

        public async Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string activityId, Instant startAt, CancellationToken cancellationToken = default)
        {
            var trigger = CreateTrigger(workflowBlueprint, activityId).StartAt(startAt.ToDateTimeOffset()).Build();
            await ScheduleJob(trigger, cancellationToken);
        }

        public async Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string activityId, string cronExpression, CancellationToken cancellationToken = default)
        {
            var trigger = CreateTrigger(workflowBlueprint, activityId).WithCronSchedule(cronExpression).Build();
            await ScheduleJob(trigger, cancellationToken);
        }

        public async Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string workflowInstanceId, string activityId, Instant startAt, CancellationToken cancellationToken = default)
        {
            var trigger = CreateTrigger(workflowBlueprint, activityId, workflowInstanceId)
                .StartAt(startAt.ToDateTimeOffset()).Build();

            await ScheduleJob(trigger, cancellationToken);
        }

        public async Task UnscheduleWorkflowAsync(WorkflowExecutionContext workflowExecutionContext, string activityId, CancellationToken cancellationToken = default)
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var trigger = CreateTriggerKey(tenantId: workflowExecutionContext.WorkflowBlueprint.TenantId,
                workflowDefinitionId: workflowExecutionContext.WorkflowBlueprint.Id,
                workflowInstanceId: workflowExecutionContext.WorkflowInstance.Id,
                activityId: activityId);

            var existingTrigger = await scheduler.GetTrigger(trigger, cancellationToken);

            if (existingTrigger != null)
                await scheduler.UnscheduleJob(existingTrigger.Key, cancellationToken);
        }

        private async Task ScheduleJob(ITrigger trigger, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
                var existingTrigger = await scheduler.GetTrigger(trigger.Key, cancellationToken);

                if (existingTrigger != null)
                    await scheduler.UnscheduleJob(existingTrigger.Key, cancellationToken);

                await scheduler.ScheduleJob(trigger, cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private TriggerBuilder CreateTrigger(IWorkflowBlueprint workflowBlueprint, string activityId) => CreateTrigger(workflowBlueprint.TenantId, workflowBlueprint.Id, null, activityId);
        private TriggerBuilder CreateTrigger(IWorkflowBlueprint workflowBlueprint, string activityId, string workflowInstanceId) => CreateTrigger(workflowBlueprint.TenantId, workflowBlueprint.Id, workflowInstanceId, activityId);

        private TriggerBuilder CreateTrigger(string? tenantId, string workflowDefinitionId, string? workflowInstanceId, string activityId)
        {
            return TriggerBuilder.Create()
                .ForJob(RunWorkflowJobKey)
                .WithIdentity(CreateTriggerKey(tenantId, workflowDefinitionId, workflowInstanceId, activityId))
                .UsingJobData("TenantId", tenantId!)
                .UsingJobData("WorkflowDefinitionId", workflowDefinitionId)
                .UsingJobData("WorkflowInstanceId", workflowInstanceId!)
                .UsingJobData("ActivityId", activityId);
        }

        private TriggerKey CreateTriggerKey(string? tenantId, string workflowDefinitionId, string? workflowInstanceId, string activityId)
        {
            var groupName = $"tenant:{tenantId ?? "default"}-workflow-instance:{workflowInstanceId ?? workflowDefinitionId}";
            return new TriggerKey($"activity:{activityId}", groupName);
        }
    }
}
