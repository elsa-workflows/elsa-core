﻿using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.Activities.Temporal.Quartz.Jobs;
using Microsoft.Extensions.Logging;
using NodaTime;
using Quartz;
using Quartz.Impl.Matchers;

namespace Elsa.Activities.Temporal.Quartz.Services
{
    public class QuartzWorkflowDefinitionScheduler : IWorkflowDefinitionScheduler
    {
        private static readonly string RunWorkflowJobKey = nameof(RunQuartzWorkflowDefinitionJob);
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _semaphore = new(1);

        public QuartzWorkflowDefinitionScheduler(ISchedulerFactory schedulerFactory, ILogger<QuartzWorkflowDefinitionScheduler> logger)
        {
            _schedulerFactory = schedulerFactory;
            _logger = logger;
        }

        public async Task ScheduleAsync(string workflowDefinitionId, string activityId, Instant startAt, Duration? interval, CancellationToken cancellationToken = default)
        {
            var triggerBuilder = CreateTrigger(workflowDefinitionId, activityId).StartAt(startAt.ToDateTimeOffset());
            
            if (interval != null && interval != Duration.Zero)
                triggerBuilder.WithSimpleSchedule(x => x.WithInterval(interval.Value.ToTimeSpan()).RepeatForever());

            var trigger = triggerBuilder.Build();
            await ScheduleJob(trigger, cancellationToken);
        }

        public async Task ScheduleAsync(string workflowDefinitionId, string activityId, string cronExpression, CancellationToken cancellationToken = default)
        {
            var trigger = CreateTrigger(workflowDefinitionId, activityId).WithCronSchedule(cronExpression).Build();
            await ScheduleJob(trigger, cancellationToken);
        }
        
        public async Task UnscheduleAsync(string workflowDefinitionId, string activityId, CancellationToken cancellationToken)
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var trigger = CreateTriggerKey(workflowDefinitionId, activityId);
            var existingTrigger = await scheduler.GetTrigger(trigger, cancellationToken);

            if (existingTrigger != null)
                await scheduler.UnscheduleJob(existingTrigger.Key, cancellationToken);
        }
        
        public async Task UnscheduleAsync(string workflowDefinitionId, CancellationToken cancellationToken)
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var groupName = CreateTriggerGroupKey(workflowDefinitionId);
            var existingTriggers = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.GroupEquals(groupName), cancellationToken);

            foreach (var existingTrigger in existingTriggers) 
                await scheduler.UnscheduleJob(existingTrigger, cancellationToken);
        }

        public async Task UnscheduleAllAsync(CancellationToken cancellationToken)
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupStartsWith("workflow-definition"), cancellationToken);
            await scheduler.DeleteJobs(jobKeys, cancellationToken);
        }

        private async Task ScheduleJob(ITrigger trigger, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
                var existingTrigger = await scheduler.GetTrigger(trigger.Key, cancellationToken);

                // For workflow definitions we only schedule the job if one doesn't exist already because another node may have created it beforehand.
                if (existingTrigger == null)
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

        private TriggerBuilder CreateTrigger(string workflowDefinitionId, string activityId) =>
            TriggerBuilder.Create()
                .ForJob(RunWorkflowJobKey)
                .WithIdentity(CreateTriggerKey(workflowDefinitionId, activityId))
                .UsingJobData("WorkflowDefinitionId", workflowDefinitionId!)
                .UsingJobData("ActivityId", activityId);
        
        private TriggerKey CreateTriggerKey(string workflowDefinitionId, string activityId)
        {
            var groupName = CreateTriggerGroupKey(workflowDefinitionId);
            return new TriggerKey($"activity:{activityId}", groupName);
        }
        
        private string CreateTriggerGroupKey(string workflowDefinitionId) => $"workflow-definition:{workflowDefinitionId}";
    }
}