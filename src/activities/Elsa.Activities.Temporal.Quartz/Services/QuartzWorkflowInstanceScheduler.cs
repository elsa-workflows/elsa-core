using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.Activities.Temporal.Quartz.Jobs;
using Medallion.Threading;
using Microsoft.Extensions.Logging;
using NodaTime;
using Quartz;
using Quartz.Impl.Matchers;
using IDistributedLockProvider = Elsa.Services.IDistributedLockProvider;

namespace Elsa.Activities.Temporal.Quartz.Services
{
    public class QuartzWorkflowInstanceScheduler : IWorkflowInstanceScheduler
    {
        private static readonly string RunWorkflowJobKey = nameof(RunQuartzWorkflowInstanceJob);
        private readonly QuartzSchedulerProvider _schedulerProvider;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ElsaOptions _elsaOptions;
        private readonly ILogger _logger;

        public QuartzWorkflowInstanceScheduler(QuartzSchedulerProvider schedulerProvider, IDistributedLockProvider distributedLockProvider, ElsaOptions elsaOptions, ILogger<QuartzWorkflowInstanceScheduler> logger)
        {
            _schedulerProvider = schedulerProvider;
            _distributedLockProvider = distributedLockProvider;
            _elsaOptions = elsaOptions;
            _logger = logger;
        }

        public async Task ScheduleAsync(string workflowInstanceId, string activityId, Instant startAt, Duration? interval, CancellationToken cancellationToken = default)
        {
            var triggerBuilder = CreateTrigger(workflowInstanceId, activityId).StartAt(startAt.ToDateTimeOffset());
            
            if (interval != null && interval != Duration.Zero)
                triggerBuilder.WithSimpleSchedule(x => x.WithInterval(interval.Value.ToTimeSpan()).RepeatForever());

            var trigger = triggerBuilder.Build();
            await ScheduleJob(trigger, cancellationToken);
        }

        public async Task ScheduleAsync(string workflowInstanceId, string activityId, string cronExpression, CancellationToken cancellationToken = default)
        {
            var trigger = CreateTrigger(workflowInstanceId, activityId).WithCronSchedule(cronExpression).Build();
            await ScheduleJob(trigger, cancellationToken);
        }
        
        public async Task UnscheduleAsync(string workflowInstanceId, string activityId, CancellationToken cancellationToken)
        {
            var scheduler = await _schedulerProvider. GetSchedulerAsync(cancellationToken);
            var trigger = CreateTriggerKey(workflowInstanceId, activityId);
            var existingTrigger = await scheduler.GetTrigger(trigger, cancellationToken);

            if (existingTrigger != null)
                await scheduler.UnscheduleJob(existingTrigger.Key, cancellationToken);
        }
        
        public async Task UnscheduleAsync(string workflowInstanceId, CancellationToken cancellationToken)
        {
            var scheduler = await _schedulerProvider. GetSchedulerAsync(cancellationToken);
            var groupName = CreateTriggerGroupKey(workflowInstanceId);
            var existingTriggers = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.GroupEquals(groupName), cancellationToken);

            foreach (var existingTrigger in existingTriggers) 
                await scheduler.UnscheduleJob(existingTrigger, cancellationToken);
        }

        public async Task UnscheduleAllAsync(CancellationToken cancellationToken)
        {
            var scheduler = await _schedulerProvider. GetSchedulerAsync(cancellationToken);
            var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupStartsWith("workflow-instance"), cancellationToken);
            await scheduler.DeleteJobs(jobKeys, cancellationToken);
        }

        private async Task ScheduleJob(ITrigger trigger, CancellationToken cancellationToken)
        {
            var scheduler = await _schedulerProvider.GetSchedulerAsync(cancellationToken);
            var sharedResource = $"{nameof(QuartzWorkflowInstanceScheduler)}:{trigger.Key}";
            await using var handle = await _distributedLockProvider.AcquireLockAsync(sharedResource, _elsaOptions.DistributedLockTimeout, cancellationToken);

            if (handle == null)
                return;

            try
            {
                var existingTrigger = await scheduler.GetTrigger(trigger.Key, cancellationToken);

                if (existingTrigger != null)
                    await scheduler.UnscheduleJob(existingTrigger.Key, cancellationToken);
                
                await scheduler.ScheduleJob(trigger, cancellationToken);
            }
            catch (SchedulerException e)
            {
                _logger.LogWarning(e, "Failed to schedule trigger {TriggerKey}", trigger.Key.ToString());
            }
        }

        private TriggerBuilder CreateTrigger(string workflowInstanceId, string activityId) =>
            TriggerBuilder.Create()
                .ForJob(RunWorkflowJobKey)
                .WithIdentity(CreateTriggerKey(workflowInstanceId, activityId))
                .UsingJobData("WorkflowInstanceId", workflowInstanceId!)
                .UsingJobData("ActivityId", activityId);
        
        private TriggerKey CreateTriggerKey(string workflowInstanceId, string activityId)
        {
            var groupName = CreateTriggerGroupKey(workflowInstanceId);
            return new TriggerKey($"activity:{activityId}", groupName);
        }

        private string CreateTriggerGroupKey(string workflowInstanceId) => $"workflow-instance:{workflowInstanceId}";
    }
}