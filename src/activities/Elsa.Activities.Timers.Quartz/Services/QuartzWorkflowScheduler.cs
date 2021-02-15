using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Timers.Quartz.Jobs;
using Elsa.Activities.Timers.Services;
using Microsoft.Extensions.Logging;
using NodaTime;
using Quartz;

namespace Elsa.Activities.Timers.Quartz.Services
{
    public class QuartzWorkflowScheduler : IWorkflowScheduler
    {
        private static readonly string RunWorkflowJobKey = nameof(RunQuartzWorkflowJob);
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _semaphore = new(1);

        public QuartzWorkflowScheduler(ISchedulerFactory schedulerFactory, ILogger<QuartzWorkflowScheduler> logger)
        {
            _schedulerFactory = schedulerFactory;
            _logger = logger;
        }

        public async Task ScheduleWorkflowAsync(string? workflowDefinitionId, string? workflowInstanceId, string activityId, string? tenantId, Instant startAt, Duration? interval, CancellationToken cancellationToken)
        {
            var triggerBuilder = CreateTrigger(workflowDefinitionId, workflowInstanceId, activityId, tenantId).StartAt(startAt.ToDateTimeOffset());
            
            if (interval != null && interval != Duration.Zero)
                triggerBuilder.WithSimpleSchedule(x => x.WithInterval(interval.Value.ToTimeSpan()).RepeatForever());

            var trigger = triggerBuilder.Build();
            await ScheduleJob(trigger, cancellationToken);
        }

        public async Task ScheduleWorkflowAsync(string? workflowDefinitionId, string? workflowInstanceId, string activityId, string? tenantId, string cronExpression, CancellationToken cancellationToken)
        {
            var trigger = CreateTrigger(workflowDefinitionId, workflowInstanceId, activityId, tenantId).WithCronSchedule(cronExpression).Build();
            await ScheduleJob(trigger, cancellationToken);
        }
        
        public async Task UnscheduleWorkflowAsync(string? workflowDefinitionId, string? workflowInstanceId, string activityId, string? tenantId, CancellationToken cancellationToken)
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var trigger = CreateTriggerKey(tenantId, workflowDefinitionId, workflowInstanceId, activityId);
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
            catch (SchedulerException e)
            {
                _logger.LogWarning(e, "Failed to schedule trigger {TriggerKey}", trigger.Key.ToString());
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private TriggerBuilder CreateTrigger(string? workflowDefinitionId, string? workflowInstanceId, string activityId, string? tenantId) =>
            TriggerBuilder.Create()
                .ForJob(RunWorkflowJobKey)
                .WithIdentity(CreateTriggerKey(tenantId, workflowDefinitionId, workflowInstanceId, activityId))
                .UsingJobData("TenantId", tenantId!)
                .UsingJobData("WorkflowDefinitionId", workflowDefinitionId!)
                .UsingJobData("WorkflowInstanceId", workflowInstanceId!)
                .UsingJobData("ActivityId", activityId);

        private TriggerKey CreateTriggerKey(string? tenantId, string? workflowDefinitionId, string? workflowInstanceId, string activityId)
        {
            var groupName = $"tenant:{tenantId ?? "default"}-workflow-instance:{workflowInstanceId ?? workflowDefinitionId}";
            return new TriggerKey($"activity:{activityId}", groupName);
        }
    }
}