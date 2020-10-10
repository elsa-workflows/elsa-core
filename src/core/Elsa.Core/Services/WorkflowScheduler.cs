using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Indexes;
using Elsa.Messaging.Distributed;
using Elsa.Messaging.Domain;
using Elsa.Models;
using Elsa.Queries;
using Elsa.Services.Models;
using MediatR;
using Open.Linq.AsyncExtensions;
using Rebus.Bus;
using YesSql;

namespace Elsa.Services
{
    public class WorkflowScheduler : IWorkflowScheduler, INotificationHandler<WorkflowCompleted>
    {
        private readonly IBus _serviceBus;
        private readonly ISession _session;
        private readonly IWorkflowActivator _workflowActivator;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowSchedulerQueue _queue;

        public WorkflowScheduler(
            IBus serviceBus,
            ISession session,
            IWorkflowActivator workflowActivator,
            IWorkflowRegistry workflowRegistry,
            IWorkflowSchedulerQueue queue)
        {
            _serviceBus = serviceBus;
            _session = session;
            _workflowActivator = workflowActivator;
            _workflowRegistry = workflowRegistry;
            _queue = queue;
        }

        public async Task ScheduleWorkflowAsync(string instanceId,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default) =>
            await _serviceBus.Publish(new RunWorkflow(instanceId, activityId, input));

        public async Task ScheduleNewWorkflowAsync(
            string definitionId,
            object? input = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var workflow = await _workflowRegistry.GetWorkflowAsync(
                definitionId,
                VersionOptions.Published,
                cancellationToken);
            var startActivities = workflow.GetStartActivities();

            foreach (var activity in startActivities)
                await ScheduleWorkflowAsync(workflow, activity, input, correlationId, cancellationToken);
        }

        public async Task TriggerWorkflowsAsync(
            string activityType,
            object? input = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default)
        {
            await ScheduleSuspendedWorkflowsAsync(
                activityType,
                input,
                correlationId,
                cancellationToken);

            await ScheduleNewWorkflowsAsync(
                activityType,
                input,
                correlationId,
                cancellationToken);
        }

        /// <summary>
        /// Find workflows with the specified activity type as workflow triggers.
        /// </summary>
        private async Task ScheduleNewWorkflowsAsync(
            string activityType,
            object? input = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var workflows = await _workflowRegistry.GetWorkflowsAsync(cancellationToken);

            var query =
                from workflow in workflows
                where workflow.IsPublished && workflow.IsEnabled
                from activity in workflow.GetStartActivities()
                where activity.Type == activityType
                select (workflow, activity);

            var tuples = (IList<(Workflow Workflow, IActivity Activity)>)query.ToList();

            tuples = await FilterRunningSingletonsAsync(tuples).ToList();

            foreach (var (workflow, activity) in tuples)
            {
                var startedInstances = await GetStartedWorkflowsAsync(workflow).ToList();

                if (startedInstances.Any())
                {
                    // There's already a workflow instance pending to be started, so queue this workflow for launch right after the current instance completes. 
                    _queue.Enqueue(workflow, activity, input, correlationId);
                }
                else
                {
                    var workflowInstance = await _workflowActivator.ActivateAsync(
                        workflow,
                        correlationId,
                        cancellationToken);

                    _session.Save(workflowInstance);

                    await ScheduleWorkflowAsync(
                        workflowInstance.WorkflowInstanceId,
                        activity.Id,
                        input,
                        cancellationToken);
                }
            }
        }

        /// <summary>
        /// Find suspended workflow instances that are blocked on activities with the specified activity type.
        /// </summary>
        private async Task ScheduleSuspendedWorkflowsAsync(
            string activityType,
            object? input,
            string? correlationId,
            CancellationToken cancellationToken)
        {
            Expression<Func<WorkflowInstanceBlockingActivitiesIndex, bool>> predicate;

            if (!string.IsNullOrWhiteSpace(correlationId))
                predicate = x => x.ActivityType == activityType && x.CorrelationId == correlationId;
            else
                predicate = x => x.ActivityType == activityType;

            var query = _session.QueryWorkflowInstances().With<WorkflowInstanceBlockingActivitiesIndex>()
                .Where(predicate);

            var workflowInstances = await query.ListAsync();
            var tuples = workflowInstances.GetBlockingActivities();

            foreach (var (workflowInstance, blockingActivity) in tuples)
                await ScheduleWorkflowAsync(
                    workflowInstance.WorkflowInstanceId,
                    blockingActivity.ActivityId,
                    input,
                    cancellationToken);
        }

        private async Task ScheduleWorkflowAsync(Workflow workflow,
            IActivity activity,
            object? input,
            string? correlationId,
            CancellationToken cancellationToken)
        {
            var workflowInstance = await _workflowActivator.ActivateAsync(workflow, correlationId, cancellationToken);
            _session.Save(workflowInstance);
            await ScheduleWorkflowAsync(workflowInstance.WorkflowInstanceId, activity.Id, input, cancellationToken);
        }

        private async Task<IEnumerable<(Workflow, IActivity)>> FilterRunningSingletonsAsync(
            IEnumerable<(Workflow Workflow, IActivity Activity)> tuples)
        {
            var tupleList = tuples.ToList();
            var transients = tupleList.Where(x => !x.Workflow.IsSingleton).ToList();
            var singletons = tupleList.Where(x => x.Workflow.IsSingleton).ToList();
            var result = transients.ToList();

            foreach (var tuple in singletons)
            {
                var workflowDefinitionId = tuple.Workflow.WorkflowDefinitionId;

                var instances = await _session
                    .QueryWorkflowInstanceByDefinitionAndStatus(workflowDefinitionId, WorkflowStatus.Suspended)
                    .ListAsync();

                if (!instances.Any())
                    result.Add(tuple);
            }

            return result;
        }

        private async Task<IEnumerable<WorkflowInstance>> GetStartedWorkflowsAsync(Workflow workflow)
        {
            var workflowDefinitionId = workflow.WorkflowDefinitionId;

            var suspendedInstances = await _session
                .QueryWorkflowInstanceByDefinitionAndStatus(workflowDefinitionId, WorkflowStatus.Suspended)
                .ListAsync();

            var idleInstances = await _session
                .QueryWorkflowInstanceByDefinitionAndStatus(workflowDefinitionId, WorkflowStatus.Idle)
                .ListAsync();

            var startActivities = workflow.GetStartActivities().Select(x => x.Id).ToList();

            var startedInstances = suspendedInstances
                .Where(x => x.BlockingActivities.Any(y => startActivities.Contains(y.ActivityId))).ToList();

            return idleInstances.Concat(startedInstances);
        }

        public async Task Handle(WorkflowCompleted notification, CancellationToken cancellationToken)
        {
            var workflowExecutionContext = notification.WorkflowExecutionContext;
            var workflowDefinitionId = workflowExecutionContext.WorkflowDefinitionId;
            var startActivityId = workflowExecutionContext.ExecutionLog.Select(x => x.Activity.Id).FirstOrDefault();

            if (startActivityId == null)
                return;

            var entry = _queue.Dequeue(workflowDefinitionId, startActivityId);
            if (entry == null)
                return;

            await ScheduleWorkflowAsync(
                entry.Value.Workflow,
                entry.Value.Activity,
                entry.Value.Input,
                entry.Value.CorrelationId,
                cancellationToken);
        }
    }
}