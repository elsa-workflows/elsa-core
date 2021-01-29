using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Timers.Bookmarks;
using Elsa.Activities.Timers.Services;
using Elsa.Activities.Timers.Triggers;
using Elsa.Bookmarks;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace Elsa.Activities.Timers.StartupTasks
{
    /// <summary>
    /// Starts Quartz jobs based on workflow blueprints starting with a TimerEvent, CronEvent or StartAtEvent.
    /// </summary>
    public class StartJobs : IStartupTask
    {
        // TODO: Figure out how to start jobs across multiple tenants / how to get a list of all tenants. 
        private const string TenantId = default;

        private readonly IWorkflowSelector _workflowSelector;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowBlueprintReflector _workflowBlueprintReflector;
        private readonly IWorkflowScheduler _workflowScheduler;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IClock _clock;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public StartJobs(
            IWorkflowSelector workflowSelector, 
            IWorkflowRegistry workflowRegistry, 
            IWorkflowBlueprintReflector workflowBlueprintReflector, 
            IWorkflowScheduler workflowScheduler, 
            IWorkflowInstanceStore workflowInstanceStore,
            IClock clock,
            IServiceScopeFactory serviceScopeFactory)
        {
            _workflowSelector = workflowSelector;
            _workflowRegistry = workflowRegistry;
            _workflowBlueprintReflector = workflowBlueprintReflector;
            _workflowScheduler = workflowScheduler;
            _workflowInstanceStore = workflowInstanceStore;
            _clock = clock;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public int Order => 2000;

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var workflows = await _workflowRegistry.GetWorkflowsAsync(cancellationToken).ToListAsync(cancellationToken);

            using var scope = _serviceScopeFactory.CreateScope();
            await ScheduleTimerEventWorkflowsAsync(workflows, scope, cancellationToken);
            await ScheduleCronEventWorkflowsAsync(workflows, scope, cancellationToken);
            await ScheduleStartAtWorkflowsAsync(workflows, scope, cancellationToken);
        }

        private async Task ScheduleStartAtWorkflowsAsync(IEnumerable<IWorkflowBlueprint> workflows, IServiceScope scope, CancellationToken cancellationToken)
        {
            // Schedule workflow blueprints that start with a run-at.
            var runAtWorkflows =
                from workflow in workflows
                from activity in workflow.GetStartActivities<StartAt>()
                select (workflow, activity);

            foreach (var runAtWorkflow in runAtWorkflows)
            {
                var workflow = runAtWorkflow.workflow;
                
                if (workflow.IsSingleton && await HasRunningInstancesAsync(workflow.Id, cancellationToken))
                    continue;
                
                var activity = runAtWorkflow.activity;
                var workflowWrapper = await _workflowBlueprintReflector.ReflectAsync(scope, workflow, cancellationToken);
                var timerWrapper = workflowWrapper.GetActivity<StartAt>(activity.Id)!;
                var startAt = await timerWrapper.GetPropertyValueAsync(x => x.Instant, cancellationToken);

                await _workflowScheduler.ScheduleWorkflowAsync(workflow.Id, null, activity.Id, workflow.TenantId, startAt, null, cancellationToken);
            }

            // Schedule workflow instances that are blocked on a start-at.
            var startAtTriggers = await _workflowSelector.SelectWorkflowsAsync<StartAt>(TenantId, cancellationToken);

            foreach (var result in startAtTriggers)
            {
                var trigger = (StartAtBookmark) result.Bookmark;
                await _workflowScheduler.ScheduleWorkflowAsync(null, result.WorkflowInstanceId!, result.ActivityId, TenantId, trigger.ExecuteAt, null, cancellationToken);
            }
        }

        private async Task ScheduleTimerEventWorkflowsAsync(IEnumerable<IWorkflowBlueprint> workflows, IServiceScope scope, CancellationToken cancellationToken)
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
                
                if (workflow.IsSingleton && await HasRunningInstancesAsync(workflow.Id, cancellationToken))
                    continue;
                
                var activity = timerWorkflow.activity;
                var workflowWrapper = await _workflowBlueprintReflector.ReflectAsync(scope, workflow, cancellationToken);
                var timerEventWrapper = workflowWrapper.GetActivity<Timer>(activity.Id)!;
                var timeOut = await timerEventWrapper.GetPropertyValueAsync(x => x.Timeout, cancellationToken);
                var startAt = now.Plus(timeOut);

                await _workflowScheduler.ScheduleWorkflowAsync(workflow.Id, null, activity.Id, workflow.TenantId, startAt, timeOut, cancellationToken);
            }

            // Schedule workflow instances that are blocked on a timer.
            var timerEventTriggers = await _workflowSelector.SelectWorkflowsAsync<Timer>(TenantId, cancellationToken);

            foreach (var result in timerEventTriggers)
            {
                var trigger = (TimerBookmark) result.Bookmark;
                await _workflowScheduler.ScheduleWorkflowAsync(null, result.WorkflowInstanceId!, result.ActivityId, TenantId, trigger.ExecuteAt, null, cancellationToken);
            }
        }

        private async Task ScheduleCronEventWorkflowsAsync(IEnumerable<IWorkflowBlueprint> workflows, IServiceScope scope, CancellationToken cancellationToken)
        {
            // Schedule workflow blueprints starting with a cron.
            var cronWorkflows =
                from workflow in workflows
                from activity in workflow.GetStartActivities<Cron>()
                select (workflow, activity);

            foreach (var cronWorkflow in cronWorkflows)
            {
                var workflow = cronWorkflow.workflow;
                
                if (workflow.IsSingleton && await HasRunningInstancesAsync(workflow.Id, cancellationToken))
                    continue;
                
                var activity = cronWorkflow.activity;
                var workflowWrapper = await _workflowBlueprintReflector.ReflectAsync(scope, workflow, cancellationToken);
                var timerEventWrapper = workflowWrapper.GetActivity<Cron>(activity.Id)!;
                var cronExpression = await timerEventWrapper.GetPropertyValueAsync(x => x.CronExpression, cancellationToken);

                await _workflowScheduler.ScheduleWorkflowAsync(workflow.Id, null, activity.Id, workflow.TenantId, cronExpression!, cancellationToken);
            }

            // Schedule workflow instances blocked on a cron event.
            var cronEventTriggers = await _workflowSelector.SelectWorkflowsAsync<Cron>(TenantId, cancellationToken);

            foreach (var result in cronEventTriggers)
            {
                var trigger = (CronBookmark) result.Bookmark;
                await _workflowScheduler.ScheduleWorkflowAsync(null, result.WorkflowInstanceId!, result.ActivityId, TenantId, trigger.ExecuteAt, null, cancellationToken);
            }
        }
        
        private async Task<bool> HasRunningInstancesAsync(string workflowDefinitionId, CancellationToken cancellationToken)
        {
            var workflowInstanceCount = await _workflowInstanceStore.CountAsync(new WorkflowDefinitionIdSpecification(workflowDefinitionId).And(new WorkflowIsAlreadyExecutingSpecification()), cancellationToken);
            return workflowInstanceCount > 0;
        }
    }
}