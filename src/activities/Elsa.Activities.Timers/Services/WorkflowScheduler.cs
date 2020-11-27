﻿using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Timers.Jobs;
using Elsa.Services.Models;
using NodaTime;
using Quartz;

namespace Elsa.Activities.Timers.Services
{
    public class WorkflowScheduler : IWorkflowScheduler
    {
        private static readonly string RunWorkflowJobKey = nameof(RunWorkflowJob);
        private readonly ISchedulerFactory _schedulerFactory;

        public WorkflowScheduler(ISchedulerFactory schedulerFactory)
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
            var trigger = CreateTrigger(workflowBlueprint, activityId, workflowInstanceId).StartAt(startAt.ToDateTimeOffset()).Build();
            await ScheduleJob(trigger, cancellationToken);
        }

        private async Task ScheduleJob(ITrigger trigger, CancellationToken cancellationToken)
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            await scheduler.ScheduleJob(trigger, cancellationToken);
        }

        private TriggerBuilder CreateTrigger(IWorkflowBlueprint workflowBlueprint, string activityId) => CreateTrigger(workflowBlueprint.TenantId, workflowBlueprint.Id, null, activityId);
        private TriggerBuilder CreateTrigger(IWorkflowBlueprint workflowBlueprint, string activityId, string workflowInstanceId) => CreateTrigger(workflowBlueprint.TenantId, workflowBlueprint.Id, workflowInstanceId, activityId);

        private TriggerBuilder CreateTrigger(string? tenantId, string workflowDefinitionId, string? workflowInstanceId, string activityId)
        {
            var groupName = $"tenant:{tenantId ?? "default"}-workflow-instance:{workflowInstanceId ?? workflowDefinitionId}";

            return TriggerBuilder.Create()
                .ForJob(RunWorkflowJobKey)
                .WithIdentity($"activity:{activityId}", groupName)
                .UsingJobData("TenantId", tenantId!)
                .UsingJobData("WorkflowDefinitionId", workflowDefinitionId)
                .UsingJobData("WorkflowInstanceId", workflowInstanceId!)
                .UsingJobData("ActivityId", activityId);
        }
    }
}