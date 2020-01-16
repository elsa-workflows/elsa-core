using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Comparers;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Messages.Distributed;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services.Models;
using NodaTime;
using Rebus.Bus;
using ScheduledActivity = Elsa.Services.Models.ScheduledActivity;

namespace Elsa.Services
{
    public class WorkflowScheduler : IWorkflowScheduler
    {
        private readonly IBus serviceBus;
        private readonly IWorkflowActivator workflowActivator;
        private readonly IWorkflowRegistry workflowRegistry;
        private readonly IWorkflowInstanceStore workflowInstanceStore;

        public WorkflowScheduler(
            IBus serviceBus,
            IWorkflowActivator workflowActivator,
            IWorkflowRegistry workflowRegistry,
            IWorkflowInstanceStore workflowInstanceStore)
        {
            this.serviceBus = serviceBus;
            this.workflowActivator = workflowActivator;
            this.workflowRegistry = workflowRegistry;
            this.workflowInstanceStore = workflowInstanceStore;
        }

        public async Task ScheduleWorkflowAsync(string instanceId, string? activityId = default, object? input = default, CancellationToken cancellationToken = default)
        {
            await serviceBus.Publish(new RunWorkflow(instanceId, activityId, Variable.From(input)));
        }

        public async Task ScheduleNewWorkflowAsync(
            string definitionId,
            object? input = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var workflow = await workflowRegistry.GetWorkflowAsync(definitionId, VersionOptions.Published, cancellationToken);
            var startActivities = workflow.GetStartActivities();

            foreach (var activity in startActivities)
            {
                var workflowInstance = await workflowActivator.ActivateAsync(workflow, correlationId, cancellationToken);
                await workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
                await ScheduleWorkflowAsync(workflowInstance.Id, activity.Id, input, cancellationToken);
            }
        }

        public async Task TriggerWorkflowsAsync(
            string activityType,
            object? input = default,
            string? correlationId = default,
            Func<Variables, bool>? activityStatePredicate = default,
            CancellationToken cancellationToken = default)
        {
            await ScheduleNewWorkflowsAsync(activityType, input, correlationId, activityStatePredicate, cancellationToken);
            await ScheduleSuspendedWorkflowsAsync(activityType, input, correlationId, activityStatePredicate, cancellationToken);
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

            var tuples = (IList<(Workflow, IActivity)>)query.ToList();

            tuples = (await FilterRunningSingletonsAsync(tuples, cancellationToken)).ToList();

            foreach (var (workflow, activity) in tuples)
            {
                var workflowInstance = await workflowActivator.ActivateAsync(workflow, correlationId, cancellationToken);
                await workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
                await ScheduleWorkflowAsync(workflowInstance.Id, activity.Id, input, cancellationToken);
            }
        }

        /// <summary>
        /// Find suspended workflow instances that are blocked on activities with the specified activity type.
        /// </summary>
        private async Task ScheduleSuspendedWorkflowsAsync(string activityType, object? input, string? correlationId, Func<Variables, bool>? activityStatePredicate, CancellationToken cancellationToken)
        {
            var tuples = await workflowInstanceStore.ListByBlockingActivityAsync(activityType, correlationId, cancellationToken);

            foreach (var (workflowInstance, blockingActivity) in tuples)
            {
                var workflow = await workflowRegistry.GetWorkflowAsync(workflowInstance.DefinitionId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);
                var activity = workflow.GetActivity(blockingActivity.ActivityId);

                if (activityStatePredicate != null)
                {
                    if (!activityStatePredicate(activity.State))
                        continue;
                }

                await ScheduleWorkflowAsync(workflowInstance.Id, blockingActivity.ActivityId, input, cancellationToken);
            }
        }

        private async Task<IEnumerable<(Workflow, IActivity)>> FilterRunningSingletonsAsync(
            IEnumerable<(Workflow, IActivity)> workflows,
            CancellationToken cancellationToken)
        {
            var definitions = workflows.ToList();
            var transients = definitions.Where(x => !x.Item1.IsSingleton).ToList();
            var singletons = definitions.Where(x => x.Item1.IsSingleton).ToList();
            var result = transients.ToList();

            foreach (var definition in singletons)
            {
                var instances = await workflowInstanceStore.ListByStatusAsync(
                    definition.Item1.DefinitionId,
                    WorkflowStatus.Suspended,
                    cancellationToken
                );

                if (!instances.Any())
                    result.Add(definition);
            }

            return result;
        }
    }
}