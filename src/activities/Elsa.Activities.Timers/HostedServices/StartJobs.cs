using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Timers.Services;
using Elsa.Activities.Timers.Triggers;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.Triggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;

namespace Elsa.Activities.Timers.HostedServices
{
    /// <summary>
    /// Starts Quartz jobs based on workflow blueprints starting with a TimerEvent, CronEvent or StartAtEvent.
    /// </summary>
    public class StartJobs : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IWorkflowBlueprintReflector _workflowBlueprintReflector;
        private readonly IWorkflowScheduler _workflowScheduler;
        private readonly IClock _clock;

        public StartJobs(IServiceProvider serviceProvider, IWorkflowBlueprintReflector workflowBlueprintReflector, IWorkflowScheduler workflowScheduler, IClock clock)
        {
            _serviceProvider = serviceProvider;
            _workflowBlueprintReflector = workflowBlueprintReflector;
            _workflowScheduler = workflowScheduler;
            _clock = clock;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var workflowRegistry = scope.ServiceProvider.GetRequiredService<IWorkflowRegistry>();
            var workflowSelector = scope.ServiceProvider.GetRequiredService<IWorkflowSelector>();
            var workflows = await workflowRegistry.GetWorkflowsAsync(stoppingToken).ToListAsync(stoppingToken);

            await ScheduleTimerEventWorkflowsAsync(scope, workflows, workflowSelector, stoppingToken);
            await ScheduleCronEventWorkflowsAsync(scope, workflows, workflowSelector, stoppingToken);
            await ScheduleStartAtWorkflowsAsync(scope, workflows, workflowSelector, stoppingToken);
        }

        private async Task ScheduleStartAtWorkflowsAsync(IServiceScope serviceScope, IEnumerable<IWorkflowBlueprint> workflows, IWorkflowSelector workflowSelector, CancellationToken cancellationToken)
        {
            // Schedule workflow blueprints that start with a run-at.
            var runAtWorkflows =
                from workflow in workflows
                from activity in workflow.GetStartActivities<StartAt>()
                select (workflow, activity);

            foreach (var runAtWorkflow in runAtWorkflows)
            {
                var workflow = runAtWorkflow.workflow;
                var activity = runAtWorkflow.activity;
                var workflowWrapper = await _workflowBlueprintReflector.ReflectAsync(serviceScope, workflow, cancellationToken);
                var timerWrapper = workflowWrapper.GetActivity<StartAt>(activity.Id)!;
                var startAt = await timerWrapper.GetPropertyValueAsync(x => x.Instant, cancellationToken);

                await _workflowScheduler.ScheduleWorkflowAsync(workflow, activity.Id, startAt, cancellationToken);
            }

            // Schedule workflow instances that are blocked on a start-at.
            var startAtTriggers = await workflowSelector.SelectWorkflowsAsync<StartAtTrigger>(x => true, cancellationToken);

            foreach (var result in startAtTriggers)
            {
                var trigger = (StartAtTrigger) result.Trigger;
                var activity = result.WorkflowBlueprint.GetActivity(result.ActivityId)!;
                await _workflowScheduler.ScheduleWorkflowAsync(result.WorkflowBlueprint, result.WorkflowInstanceId!, activity.Id, trigger.ExecuteAt, cancellationToken);
            }
        }

        private async Task ScheduleTimerEventWorkflowsAsync(IServiceScope serviceScope, IEnumerable<IWorkflowBlueprint> workflows, IWorkflowSelector workflowSelector, CancellationToken cancellationToken)
        {
            // Schedule workflow blueprints that start with a timer.
            var timerWorkflows =
                from workflow in workflows
                from activity in workflow.GetStartActivities<Timer>()
                select (workflow, activity);

            var now = _clock.GetCurrentInstant();
            
            foreach (var timerWorkflow in timerWorkflows)
            {
                var workflow = timerWorkflow.workflow;
                var activity = timerWorkflow.activity;
                var workflowWrapper = await _workflowBlueprintReflector.ReflectAsync(serviceScope, workflow, cancellationToken);
                var timerEventWrapper = workflowWrapper.GetActivity<Timer>(activity.Id)!;
                var timeOut = await timerEventWrapper.GetPropertyValueAsync(x => x.Timeout, cancellationToken);
                var startAt = now.Plus(timeOut);

                await _workflowScheduler.ScheduleWorkflowAsync(workflow, activity.Id, startAt, timeOut, cancellationToken);
            }

            // Schedule workflow instances that are blocked on a timer.
            var timerEventTriggers = await workflowSelector.SelectWorkflowsAsync<TimerTrigger>(x => true, cancellationToken);

            foreach (var result in timerEventTriggers)
            {
                var trigger = (TimerTrigger) result.Trigger;
                var activity = result.WorkflowBlueprint.GetActivity(result.ActivityId)!;
                await _workflowScheduler.ScheduleWorkflowAsync(result.WorkflowBlueprint, result.WorkflowInstanceId!, activity.Id, trigger.ExecuteAt, cancellationToken);
            }
        }
        
        private async Task ScheduleCronEventWorkflowsAsync(IServiceScope serviceScope, IEnumerable<IWorkflowBlueprint> workflows, IWorkflowSelector workflowSelector, CancellationToken cancellationToken)
        {
            // Schedule workflow blueprints starting with a cron.
            var cronWorkflows =
                from workflow in workflows
                from activity in workflow.GetStartActivities<Cron>()
                select (workflow, activity);

            foreach (var cronWorkflow in cronWorkflows)
            {
                var workflow = cronWorkflow.workflow;
                var activity = cronWorkflow.activity;
                var workflowWrapper = await _workflowBlueprintReflector.ReflectAsync(serviceScope, workflow, cancellationToken);
                var timerEventWrapper = workflowWrapper.GetActivity<Cron>(activity.Id)!;
                var cronExpression = await timerEventWrapper.GetPropertyValueAsync(x => x.CronExpression, cancellationToken);

                await _workflowScheduler.ScheduleWorkflowAsync(workflow, activity.Id, cronExpression!, cancellationToken);
            }

            // Schedule workflow instances blocked on a cron event.
            var cronEventTriggers = await workflowSelector.SelectWorkflowsAsync<CronTrigger>(x => true, cancellationToken);

            foreach (var result in cronEventTriggers)
            {
                var trigger = (CronTrigger) result.Trigger;
                var activity = result.WorkflowBlueprint.GetActivity(result.ActivityId)!;
                await _workflowScheduler.ScheduleWorkflowAsync(result.WorkflowBlueprint, result.WorkflowInstanceId!, activity.Id, trigger.ExecuteAt, cancellationToken);
            }
        }
    }
}