using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Comparers;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services.Models;
using NodaTime;
using ScheduledActivity = Elsa.Services.Models.ScheduledActivity;

namespace Elsa.Services
{
    public class WorkflowHost : IWorkflowHost
    {
        private readonly IScheduler scheduler;
        private readonly IWorkflowRegistry workflowRegistry;
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly IExpressionEvaluator expressionEvaluator;
        private readonly IIdGenerator idGenerator;
        private readonly IClock clock;
        private readonly IServiceProvider serviceProvider;

        public WorkflowHost(
            IScheduler scheduler,
            IWorkflowRegistry workflowRegistry,
            IWorkflowInstanceStore workflowInstanceStore,
            IExpressionEvaluator expressionEvaluator,
            IIdGenerator idGenerator,
            IClock clock,
            IServiceProvider serviceProvider)
        {
            this.scheduler = scheduler;
            this.workflowRegistry = workflowRegistry;
            this.workflowInstanceStore = workflowInstanceStore;
            this.expressionEvaluator = expressionEvaluator;
            this.idGenerator = idGenerator;
            this.clock = clock;
            this.serviceProvider = serviceProvider;
        }

        public async Task RunAsync(string id, object? input = default, CancellationToken cancellationToken = default)
        {
            var workflow = await workflowRegistry.GetWorkflowAsync(id, VersionOptions.Published, cancellationToken);
            var instanceId = idGenerator.Generate();
            var startActivities = workflow.GetStartActivities();
            var scheduledActivities = startActivities.Select(x => new ScheduledActivity(x, input)).ToList();
            var workflowExecutionContext = new WorkflowExecutionContext(expressionEvaluator, serviceProvider, instanceId, workflow.Activities, workflow.Connections, scheduledActivities, persistenceBehavior: workflow.PersistenceBehavior);

            workflowExecutionContext = await scheduler.RunAsync(workflowExecutionContext, cancellationToken);

            var workflowInstance = CreateWorkflowInstance(workflow, workflowExecutionContext);
            await workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
        }

        public async Task TriggerAsync(string activityType, object? input = default, string? correlationId = default, Func<Variables, bool>? activityStatePredicate = default, CancellationToken cancellationToken = default)
        {
            await RunWorkflowsAsync(activityType, input, correlationId, activityStatePredicate, cancellationToken);
            await ResumeWorkflowsAsync(activityType, input, correlationId, activityStatePredicate, cancellationToken);
        }

        /// <summary>
        /// Find workflows exposing activities with the specified activity type as workflow triggers.
        /// </summary>
        private async Task RunWorkflowsAsync(
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
                var scheduledActivities = new Stack<ScheduledActivity>(new[] { new ScheduledActivity(activity, input) });
                var instanceId = idGenerator.Generate();
                var workflowExecutionContext = new WorkflowExecutionContext(expressionEvaluator, serviceProvider, instanceId, workflow.Activities, workflow.Connections, scheduledActivities, persistenceBehavior: workflow.PersistenceBehavior);

                await scheduler.RunAsync(workflowExecutionContext, cancellationToken);
            }
        }

        /// <summary>
        /// Find suspended workflow instances that are blocked on activities with the specified activity type.
        /// </summary>
        private async Task ResumeWorkflowsAsync(string activityType, object? input, string? correlationId, Func<Variables, bool>? activityStatePredicate, CancellationToken cancellationToken)
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

                var workflowExecutionContext = CreateWorkflowExecutionContext(workflow, workflowInstance);

                await scheduler.ResumeAsync(workflowExecutionContext, activity, input, cancellationToken);
            }
        }

        private ScheduledActivity CreateScheduledActivity(Elsa.Models.ScheduledActivity scheduledActivityModel, IDictionary<string, IActivity> activityLookup)
        {
            var activity = activityLookup[scheduledActivityModel.ActivityId];
            return new ScheduledActivity(activity, scheduledActivityModel.Input);
        }

        private WorkflowInstance CreateWorkflowInstance(Workflow workflow, WorkflowExecutionContext workflowExecutionContext)
        {
            var workflowInstance = new WorkflowInstance
            {
                Id = workflowExecutionContext.InstanceId,
                Variables = workflowExecutionContext.Variables,
                ScheduledActivities = new Stack<Elsa.Models.ScheduledActivity>(workflowExecutionContext.ScheduledActivities.Select(x => new Elsa.Models.ScheduledActivity(x.Activity.Id, x.Input))),
                BlockingActivities = new HashSet<BlockingActivity>(workflowExecutionContext.BlockingActivities.Select(x => new BlockingActivity(x.Id, x.Type)), new BlockingActivityEqualityComparer()),
                Status = workflowExecutionContext.Status,
                Version = workflow.Version,
                CorrelationId = workflowExecutionContext.CorrelationId,
                DefinitionId = workflow.DefinitionId,
            };

            return workflowInstance;
        }

        private WorkflowInstance UpdateWorkflowInstance(WorkflowInstance workflowInstance, WorkflowExecutionContext workflowExecutionContext)
        {
            workflowInstance.Variables = workflowExecutionContext.Variables;
            workflowInstance.ScheduledActivities = new Stack<Elsa.Models.ScheduledActivity>(workflowExecutionContext.ScheduledActivities.Select(x => new Elsa.Models.ScheduledActivity(x.Activity.Id, x.Input)));
            workflowInstance.BlockingActivities = new HashSet<BlockingActivity>(workflowExecutionContext.BlockingActivities.Select(x => new BlockingActivity(x.Id, x.Type)), new BlockingActivityEqualityComparer());
            workflowInstance.Status = workflowExecutionContext.Status;
            workflowInstance.CorrelationId = workflowExecutionContext.CorrelationId;

            return workflowInstance;
        }

        private WorkflowExecutionContext CreateWorkflowExecutionContext(Workflow workflow, WorkflowInstance workflowInstance)
        {
            var activityLookup = workflow.Activities.ToDictionary(x => x.Id);
            var scheduledActivities = new Stack<ScheduledActivity>(workflowInstance.ScheduledActivities.Reverse().Select(x => CreateScheduledActivity(x, activityLookup)));
            var blockingActivities = new HashSet<IActivity>(workflowInstance.BlockingActivities.Select(x => activityLookup[x.ActivityId]));
            var variables = workflowInstance.Variables;
            var status = workflowInstance.Status;
            var persistenceBehavior = workflow.PersistenceBehavior;
            return new WorkflowExecutionContext(expressionEvaluator, serviceProvider, workflowInstance.Id, workflow.Activities, workflow.Connections, scheduledActivities, blockingActivities, variables, status, persistenceBehavior);
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