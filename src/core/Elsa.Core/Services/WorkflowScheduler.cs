using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Messaging.Distributed;
using Elsa.Messaging.Domain;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services.Models;
using MediatR;
using Rebus.Bus;

namespace Elsa.Services
{
    public class WorkflowScheduler : IWorkflowScheduler, INotificationHandler<WorkflowCompleted>
    {
        private readonly IBus serviceBus;
        private readonly IWorkflowActivator workflowActivator;
        private readonly IWorkflowRegistry workflowRegistry;
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly IWorkflowSchedulerQueue queue;

        public WorkflowScheduler(
            IBus serviceBus,
            IWorkflowActivator workflowActivator,
            IWorkflowRegistry workflowRegistry,
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowSchedulerQueue queue)
        {
            this.serviceBus = serviceBus;
            this.workflowActivator = workflowActivator;
            this.workflowRegistry = workflowRegistry;
            this.workflowInstanceStore = workflowInstanceStore;
            this.queue = queue;
        }

        public async Task ScheduleWorkflowAsync(string instanceId, string? activityId = default, object? input = default, CancellationToken cancellationToken = default) => 
            await serviceBus.Publish(new RunWorkflow(instanceId, activityId, input));

        public async Task ScheduleNewWorkflowAsync(
            string definitionId,
            object? input = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var workflow = await workflowRegistry.GetWorkflowAsync(definitionId, VersionOptions.Published, cancellationToken);
            var startActivities = workflow.GetStartActivities();

            foreach (var activity in startActivities)
                await ScheduleWorkflowAsync(workflow, activity, input, correlationId, cancellationToken);
        }

        public async Task TriggerWorkflowsAsync(
            string activityType,
            object? input = default,
            string? correlationId = default,
            Func<Variables, bool>? activityStatePredicate = default,
            CancellationToken cancellationToken = default)
        {
            await ScheduleSuspendedWorkflowsAsync(activityType, input, correlationId, activityStatePredicate, cancellationToken);
            await ScheduleNewWorkflowsAsync(activityType, input, correlationId, activityStatePredicate, cancellationToken);
        }

        /// <summary>
        /// Find workflows exposing activities with the specified activity type as workflow triggers.
        /// </summary>
        private async Task ScheduleNewWorkflowsAsync(
            string activityType,
            object? input = default,
            string? correlationId = default,
            Func<Variables, bool>? activityStatePredicate = default,
            CancellationToken cancellationToken = default)
        {
            var workflows = await workflowRegistry.GetWorkflowsAsync(cancellationToken);

            var query =
                from workflow in workflows
                where workflow.IsPublished
                from activity in workflow.GetStartActivities()
                where activity.Type == activityType
                select (workflow, activity);

            if (activityStatePredicate != null)
                query = query.Where(x => activityStatePredicate(x.Item2.State));

            var tuples = (IList<(Workflow Workflow, IActivity Activity)>)query.ToList();

            tuples = await FilterRunningSingletonsAsync(tuples, cancellationToken).ToListAsync();
            
            foreach (var (workflow, activity) in tuples)
            {
                var startedInstances = await GetStartedWorkflowsAsync(workflow, cancellationToken).ToListAsync();
                
                if (startedInstances.Any())
                {
                    // There's already a workflow instance pending to be started, so queue this workflow for launch right after the current instance completes. 
                    queue.Enqueue(workflow, activity, input, correlationId);
                }
                else
                {
                    var workflowInstance = await workflowActivator.ActivateAsync(workflow, correlationId, cancellationToken);
                    await workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
                    await ScheduleWorkflowAsync(workflowInstance.Id, activity.Id, input, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Find suspended workflow instances that are blocked on activities with the specified activity type.
        /// </summary>
        private async Task ScheduleSuspendedWorkflowsAsync(string activityType, object? input, string? correlationId, Func<Variables, bool>? activityStatePredicate, CancellationToken cancellationToken)
        {
            var tuples = await workflowInstanceStore.ListByBlockingActivityAsync(activityType, correlationId, activityStatePredicate, cancellationToken);

            foreach (var (workflowInstance, blockingActivity) in tuples) 
                await ScheduleWorkflowAsync(workflowInstance.Id, blockingActivity.Id, input, cancellationToken);
        }

        private async Task ScheduleWorkflowAsync(Workflow workflow, IActivity activity, object? input, string? correlationId, CancellationToken cancellationToken)
        {
            var workflowInstance = await workflowActivator.ActivateAsync(workflow, correlationId, cancellationToken);
            await workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
            await ScheduleWorkflowAsync(workflowInstance.Id, activity.Id, input, cancellationToken);
        }

        private async Task<IEnumerable<(Workflow, IActivity)>> FilterRunningSingletonsAsync(
            IEnumerable<(Workflow Workflow, IActivity Activity)> tuples,
            CancellationToken cancellationToken)
        {
            var tupleList = tuples.ToList();
            var transients = tupleList.Where(x => !x.Workflow.IsSingleton).ToList();
            var singletons = tupleList.Where(x => x.Workflow.IsSingleton).ToList();
            var result = transients.ToList();

            foreach (var tuple in singletons)
            {
                var instances = await workflowInstanceStore.ListByStatusAsync(
                    tuple.Workflow.DefinitionId,
                    WorkflowStatus.Suspended,
                    cancellationToken
                );

                if (!instances.Any())
                    result.Add(tuple);
            }

            return result;
        }

        private async Task<IEnumerable<WorkflowInstance>> GetStartedWorkflowsAsync(
            Workflow workflow,
            CancellationToken cancellationToken)
        {
            var suspendedInstances = await workflowInstanceStore.ListByStatusAsync(workflow.DefinitionId, WorkflowStatus.Suspended, cancellationToken).ToListAsync();
            var idleInstances = await workflowInstanceStore.ListByStatusAsync(workflow.DefinitionId, WorkflowStatus.Idle, cancellationToken);
            var startActivities = workflow.GetStartActivities().Select(x => x.Id).ToList();
            var startedInstances = suspendedInstances.Where(x => x.BlockingActivities.Any(y => startActivities.Contains(y.ActivityId))).ToList();

            return idleInstances.Concat(startedInstances);
        }

        public async Task Handle(WorkflowCompleted notification, CancellationToken cancellationToken)
        {
            var workflowExecutionContext = notification.WorkflowExecutionContext;
            var workflowDefinitionId = workflowExecutionContext.DefinitionId;
            var startActivityId = workflowExecutionContext.ExecutionLog.Select(x => x.Activity.Id).FirstOrDefault();

            if (startActivityId == null)
                return;

            var entry = queue.Dequeue(workflowDefinitionId, startActivityId);
            if (entry == null)
                return;
            
            await ScheduleWorkflowAsync(entry.Value.Workflow, entry.Value.Activity, entry.Value.Input, entry.Value.CorrelationId, cancellationToken);
        }
    }
}