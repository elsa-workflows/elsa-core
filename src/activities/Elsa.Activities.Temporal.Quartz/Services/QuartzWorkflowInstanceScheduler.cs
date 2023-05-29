using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.Activities.Temporal.Quartz.Jobs;
using Elsa.Exceptions;
using Elsa.Options;
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
            using var loggingScope = _logger.BeginScope(new WorkflowInstanceLogScope(workflowInstanceId));
            var triggerBuilder = CreateTrigger(workflowInstanceId, activityId).StartAt(startAt.ToDateTimeOffset());

            if (interval != null && interval != Duration.Zero)
                triggerBuilder.WithSimpleSchedule(x => x.WithInterval(interval.Value.ToTimeSpan()).RepeatForever());

            var trigger = triggerBuilder.Build();
            await ScheduleJob(trigger, cancellationToken);
        }

        public async Task ScheduleAsync(string workflowInstanceId, string activityId, string cronExpression, CancellationToken cancellationToken = default)
        {
            using var loggingScope = _logger.BeginScope(new WorkflowInstanceLogScope(workflowInstanceId));
            var trigger = CreateTrigger(workflowInstanceId, activityId).WithCronSchedule(cronExpression).Build();
            await ScheduleJob(trigger, cancellationToken);
        }

        public async Task UnscheduleAsync(string workflowInstanceId, string activityId, CancellationToken cancellationToken)
        {
            using var loggingScope = _logger.BeginScope(new WorkflowInstanceLogScope(workflowInstanceId));
            var scheduler = await _schedulerProvider.GetSchedulerAsync(cancellationToken);
            var trigger = CreateTriggerKey(workflowInstanceId, activityId);
            var existingTrigger = await scheduler.GetTrigger(trigger, cancellationToken);

            if (existingTrigger != null)
                await scheduler.UnscheduleJob(existingTrigger.Key, cancellationToken);
        }

        public async Task UnscheduleAsync(string workflowInstanceId, CancellationToken cancellationToken)
        {
            using var loggingScope = _logger.BeginScope(new WorkflowInstanceLogScope(workflowInstanceId));
            var scheduler = await _schedulerProvider.GetSchedulerAsync(cancellationToken);
            var groupName = CreateTriggerGroupKey(workflowInstanceId);
            var existingTriggers = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.GroupEquals(groupName), cancellationToken);

            foreach (var existingTrigger in existingTriggers)
                await scheduler.UnscheduleJob(existingTrigger, cancellationToken);
        }

        public async Task UnscheduleAllAsync(CancellationToken cancellationToken)
        {
            var scheduler = await _schedulerProvider.GetSchedulerAsync(cancellationToken);
            var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupStartsWith("workflow-instance"), cancellationToken);
            await scheduler.DeleteJobs(jobKeys, cancellationToken);
        }

        private async Task ScheduleJob(ITrigger trigger, CancellationToken cancellationToken)
        {
            var scheduler = await _schedulerProvider.GetSchedulerAsync(cancellationToken);
            var sharedResource = $"{nameof(QuartzWorkflowInstanceScheduler)}:{trigger.Key}";
            var distributedLockTimeout = _elsaOptions.DistributedLockTimeout;
            await using var handle = await _distributedLockProvider.AcquireLockAsync(sharedResource, distributedLockTimeout, cancellationToken);

            if (handle == null)
                throw new LockAcquisitionException($"Failed to acquire a distributed lock within the configured amount of time ({distributedLockTimeout})");

            try
            {
                var existingTrigger = await scheduler.GetTrigger(trigger.Key, cancellationToken);

                if (existingTrigger != null)
                {
                    _logger.LogDebug("Found existing trigger with key {TriggerKey}", trigger.Key.ToString());
                    await scheduler.UnscheduleJob(existingTrigger.Key, cancellationToken);
                }

                _logger.LogDebug("Scheduling new job with key {TriggerKey}", trigger.Key.ToString());
                await scheduler.ScheduleJob(trigger, cancellationToken);
            }
            catch (SchedulerException e)
            {
                _logger.LogError(e, "Failed to schedule trigger {TriggerKey}", trigger.Key.ToString());
                throw;
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