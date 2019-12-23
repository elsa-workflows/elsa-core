using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Messages;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services.Extensions;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using NodaTime;
using ScheduledActivity = Elsa.Services.Models.ScheduledActivity;

namespace Elsa.Services
{
    public class WorkflowRunner : IWorkflowRunner
    {
        private readonly IActivityInvoker activityInvoker;
        private readonly IWorkflowFactory workflowFactory;
        private readonly IWorkflowRegistry workflowRegistry;
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly IClock clock;
        private readonly IMediator mediator;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;
        private readonly IWorkflowExpressionEvaluator workflowExpressionEvaluator;

        public WorkflowRunner(
            IActivityInvoker activityInvoker,
            IWorkflowFactory workflowFactory,
            IWorkflowRegistry workflowRegistry,
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowExpressionEvaluator workflowExpressionEvaluator,
            IClock clock,
            IMediator mediator,
            IServiceProvider serviceProvider,
            ILogger<WorkflowRunner> logger)
        {
            this.activityInvoker = activityInvoker;
            this.workflowFactory = workflowFactory;
            this.workflowRegistry = workflowRegistry;
            this.workflowInstanceStore = workflowInstanceStore;
            this.clock = clock;
            this.mediator = mediator;
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            this.workflowExpressionEvaluator = workflowExpressionEvaluator;
        }
        
        public async Task<IEnumerable<WorkflowExecutionContext>> TriggerAsync(
            string activityType,
            Variable input = default,
            string correlationId = default,
            Func<Variables, bool> activityStatePredicate = default,
            CancellationToken cancellationToken = default)
        {
            var startedExecutionContexts = await RunManyAsync(
                activityType,
                input,
                correlationId,
                activityStatePredicate,
                cancellationToken
            );

            var resumedExecutionContexts = await ResumeManyAsync(
                activityType,
                input,
                correlationId,
                activityStatePredicate,
                cancellationToken
            );

            return startedExecutionContexts.Concat(resumedExecutionContexts);
        }

        public Task<WorkflowExecutionContext> RunAsync(
            Workflow workflow,
            IEnumerable<IActivity> startActivities = default,
            CancellationToken cancellationToken = default)
        {
            return RunAsync(workflow, false, startActivities, cancellationToken);
        }

        public Task<WorkflowExecutionContext> RunAsync(
            WorkflowDefinitionVersion workflowDefinition,
            Variable input = default,
            IEnumerable<string> startActivityIds = default,
            string correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var blueprint = workflowFactory.CreateWorkflowBlueprint(workflowDefinition);
            return RunAsync(blueprint, input, startActivityIds, correlationId, cancellationToken);
        }
        
        public Task<WorkflowExecutionContext> RunAsync(
            WorkflowBlueprint workflowDefinition,
            Variable input = default,
            IEnumerable<string> startActivityIds = default,
            string correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var workflow = workflowFactory.CreateWorkflow(workflowDefinition, input, correlationId: correlationId);
            var startActivities = workflow.Blueprint.Activities.Find(startActivityIds);

            return RunAsync(workflow, false, startActivities, cancellationToken);
        }

        public Task<WorkflowExecutionContext> RunAsync<T>(
            Variable input = default,
            IEnumerable<string> startActivityIds = default,
            string correlationId = default,
            CancellationToken cancellationToken = default) where T : IWorkflow, new()
        {
            var workflow = workflowFactory.CreateWorkflow<T>(input, correlationId: correlationId);
            var startActivities = workflow.Blueprint.Activities.Find(startActivityIds);

            return RunAsync(workflow, false, startActivities, cancellationToken);
        }
        
        public Task<WorkflowExecutionContext> RunAsync(
            WorkflowInstance workflowInstance,
            Variable input = null,
            IEnumerable<string> startActivityIds = default,
            CancellationToken cancellationToken = default)
        {
            return RunAsync(workflowInstance, false, input, startActivityIds, cancellationToken);
        }

        public Task<WorkflowExecutionContext> ResumeAsync(
            Workflow workflow,
            IEnumerable<IActivity> startActivities = default,
            CancellationToken cancellationToken = default)
        {
            return RunAsync(workflow, true, startActivities, cancellationToken);
        }

        public Task<WorkflowExecutionContext> ResumeAsync<T>(
            WorkflowInstance workflowInstance,
            Variable input = null,
            IEnumerable<string> startActivityIds = default,
            CancellationToken cancellationToken = default) where T : IWorkflow, new()
        {
            var workflow = workflowFactory.CreateWorkflow<T>(input, workflowInstance);
            var startActivities = workflow.Blueprint.Activities.Find(startActivityIds);
            return RunAsync(workflow, true, startActivities, cancellationToken);
        }

        public Task<WorkflowExecutionContext> ResumeAsync(
            WorkflowInstance workflowInstance,
            Variable input = null,
            IEnumerable<string> startActivityIds = default,
            CancellationToken cancellationToken = default)
        {
            return RunAsync(workflowInstance, true, input, startActivityIds, cancellationToken);
        }
        
        private async Task<WorkflowExecutionContext> RunAsync(
            WorkflowInstance workflowInstance,
            bool resume,
            Variable input = null,
            IEnumerable<string> startActivityIds = default,
            CancellationToken cancellationToken = default)
        {
            var definition = await workflowRegistry.GetWorkflowBlueprintAsync(
                workflowInstance.DefinitionId,
                VersionOptions.SpecificVersion(workflowInstance.Version),
                cancellationToken);
            var workflow = workflowFactory.CreateWorkflow(definition, input, workflowInstance);
            return await RunAsync(workflow, resume, startActivityIds, cancellationToken);
        }

        private async Task<IEnumerable<WorkflowExecutionContext>> ResumeManyAsync(
            string activityType,
            Variable input = default,
            string correlationId = default,
            Func<Variables, bool> activityStatePredicate = default,
            CancellationToken cancellationToken = default)
        {
            var workflowInstances = await workflowInstanceStore
                .ListByBlockingActivityAsync(activityType, correlationId, cancellationToken)
                .ToListAsync();

            if (activityStatePredicate != null)
                workflowInstances = workflowInstances.Where(x => activityStatePredicate(x.Item2.State)).ToList();

            return await ResumeManyAsync(
                workflowInstances,
                input,
                cancellationToken
            );
        }

        private async Task<IEnumerable<WorkflowExecutionContext>> RunManyAsync(
            string activityType,
            Variable input = default,
            string correlationId = default,
            Func<Variables, bool> activityStatePredicate = default,
            CancellationToken cancellationToken = default)
        {
            var workflowDefinitions = await workflowRegistry.ListByStartActivityAsync(activityType, cancellationToken);

            if (activityStatePredicate != null)
                workflowDefinitions = workflowDefinitions.Where(x => activityStatePredicate(x.Item2.State));

            workflowDefinitions = await FilterRunningSingletonsAsync(
                workflowDefinitions,
                cancellationToken
            );

            return await RunManyAsync(workflowDefinitions, input, correlationId, cancellationToken);
        }

        private async Task<IEnumerable<WorkflowExecutionContext>> RunManyAsync(
            IEnumerable<(WorkflowBlueprint, IActivity)> workflowDefinitions,
            Variable input,
            string correlationId,
            CancellationToken cancellationToken1)
        {
            var executionContexts = new List<WorkflowExecutionContext>();

            foreach (var (workflowDefinition, activityDefinition) in workflowDefinitions)
            {
                var startActivityIds = workflowDefinition.Activities
                    .Where(x => x.Id == activityDefinition.Id)
                    .Select(x => x.Id);

                var workflow = workflowFactory.CreateWorkflow(workflowDefinition, input, correlationId: correlationId);

                var executionContext = await RunAsync(
                    workflow,
                    false,
                    startActivityIds,
                    cancellationToken1
                );
                executionContexts.Add(executionContext);
            }

            return executionContexts;
        }

        private async Task<IEnumerable<WorkflowExecutionContext>> ResumeManyAsync(
            IEnumerable<(WorkflowInstance, ActivityInstance)> workflowInstances,
            Variable input,
            CancellationToken cancellationToken)
        {
            var executionContexts = new List<WorkflowExecutionContext>();
            var workflowInstanceGroups = workflowInstances.GroupBy(x => x.Item1);

            foreach (var workflowInstanceGroup in workflowInstanceGroups)
            {
                var workflowInstance = workflowInstanceGroup.Key;

                var workflowDefinition = await workflowRegistry.GetWorkflowBlueprintAsync(
                    workflowInstance.DefinitionId,
                    VersionOptions.SpecificVersion(workflowInstance.Version),
                    cancellationToken
                );

                var workflow = workflowFactory.CreateWorkflow(workflowDefinition, input, workflowInstance);

                foreach (var activity in workflowInstanceGroup)
                {
                    var executionContext = await RunAsync(
                        workflow,
                        true,
                        new[] { activity.Item2.Id },
                        cancellationToken
                    );

                    executionContexts.Add(executionContext);
                }
            }

            return executionContexts;
        }
        
        private Task<WorkflowExecutionContext> RunAsync(
            Workflow workflow,
            bool resume,
            IEnumerable<string> startActivityIds = default,
            CancellationToken cancellationToken = default)
        {
            var startActivities = startActivityIds != null
                ? workflow.Blueprint.Activities.Find(startActivityIds)
                : Enumerable.Empty<IActivity>();

            return RunAsync(workflow, resume, startActivities, cancellationToken);
        }
        
        private async Task<WorkflowExecutionContext> RunAsync(
            Workflow workflow,
            bool resume,
            IEnumerable<IActivity> startActivities = default,
            CancellationToken cancellationToken = default)
        {
            await mediator.Publish(new ExecutingWorkflow(workflow), cancellationToken);
            
            var workflowExecutionContext = await CreateWorkflowExecutionContextAsync(
                workflow,
                startActivities,
                cancellationToken
            );

            var start = !resume;

            while (workflowExecutionContext.HasScheduledActivities)
            {
                var nextActivity = workflowExecutionContext.PeekScheduledActivity();
                await mediator.Publish(new ExecutingActivity(workflow, nextActivity.Activity), cancellationToken);
                var scheduledActivity = workflowExecutionContext.PopScheduledActivity();
                var activity = scheduledActivity.Activity;

                var result = start
                    ? await ExecuteActivityAsync(workflowExecutionContext, scheduledActivity, cancellationToken)
                    : await ResumeActivityAsync(workflowExecutionContext, scheduledActivity, cancellationToken);

                workflowExecutionContext.IsFirstPass = false;
                start = true;

                await mediator.Publish(new ActivityExecuted(workflow, activity), cancellationToken);
                
                if (result == null)
                    break;

                await result.ExecuteAsync(this, workflowExecutionContext, cancellationToken);
            }
            
            // Determine new workflow state.
            UpdateWorkflowStatus(workflowExecutionContext);
            
            // Publish appropriate event depending on current workflow state.
            await PublishTransitionEvent(workflowExecutionContext, cancellationToken);

            return workflowExecutionContext;
        }

        private static void UpdateWorkflowStatus( WorkflowExecutionContext workflowExecutionContext)
        {
            // Automatically transition to Suspended state if there are blocking activities.
            if (workflowExecutionContext.Workflow.BlockingActivities.Any())
                workflowExecutionContext.Suspend();

            // Automatically transition to Completed state if there are no more blocking activities. 
            else if(!workflowExecutionContext.HasScheduledActivities)
                workflowExecutionContext.Complete();
        }

        private async Task PublishTransitionEvent(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            var workflow = workflowExecutionContext.Workflow;
            
            // Publish Workflow Executed event.
            await mediator.Publish(new WorkflowExecuted(workflow), cancellationToken);
            
            switch (workflow.Status)
            {
                case WorkflowStatus.Cancelled:
                    await mediator.Publish(new WorkflowCancelled(workflow), cancellationToken);
                    break;
                case WorkflowStatus.Completed:
                    await mediator.Publish(new WorkflowCompleted(workflow), cancellationToken);
                    break;
                case WorkflowStatus.Faulted:
                    await mediator.Publish(new WorkflowFaulted(workflow), cancellationToken);
                    break;
                case WorkflowStatus.Suspended:
                    await mediator.Publish(new WorkflowSuspended(workflow), cancellationToken);
                    break;
            }
        }

        private async Task<IActivityExecutionResult> ExecuteActivityAsync(
            WorkflowExecutionContext workflowContext,
            ScheduledActivity scheduledActivity,
            CancellationToken cancellationToken)
        {
            return await InvokeActivityAsync(
                workflowContext,
                scheduledActivity.Activity,
                async () => await activityInvoker.ExecuteAsync(workflowContext, scheduledActivity.Activity, scheduledActivity.Input, cancellationToken),
                cancellationToken
            );
        }

        private async Task<IActivityExecutionResult> ResumeActivityAsync(
            WorkflowExecutionContext workflowContext,
            ScheduledActivity scheduledActivity,
            CancellationToken cancellationToken)
        {
            return await InvokeActivityAsync(
                workflowContext,
                scheduledActivity.Activity,
                async () => await activityInvoker.ResumeAsync(workflowContext, scheduledActivity.Activity, scheduledActivity.Input, cancellationToken),
                cancellationToken
            );
        }

        private async Task<IActivityExecutionResult> InvokeActivityAsync(
            WorkflowExecutionContext workflowContext,
            IActivity activity,
            Func<Task<IActivityExecutionResult>> executeAction,
            CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    workflowContext.Workflow.Status = WorkflowStatus.Cancelled;
                    workflowContext.Workflow.CompletedAt = clock.GetCurrentInstant();
                    return null;
                }

                return await executeAction();
            }
            catch (Exception ex)
            {
                FaultWorkflow(workflowContext, activity, ex);
            }

            return null;
        }

        private void FaultWorkflow(WorkflowExecutionContext workflowContext, IActivity activity, Exception ex)
        {
            logger.LogError(
                ex,
                "An unhandled error occurred while executing an activity. Putting the workflow in the faulted state."
            );
            workflowContext.Fault(activity, ex);
        }

        private async Task<WorkflowExecutionContext> CreateWorkflowExecutionContextAsync(
            Workflow workflow,
            IEnumerable<IActivity> startActivities,
            CancellationToken cancellationToken)
        {
            var workflowExecutionContext = new WorkflowExecutionContext(workflow, workflowExpressionEvaluator, clock, serviceProvider);
            var startActivityList = startActivities?.ToList() ?? workflow.GetStartActivities().Take(1).ToList();

            foreach (var startActivity in startActivityList)
            {
                var activityExecutionContext = new ActivityExecutionContext(workflowExecutionContext, workflow.Input);
                if (await startActivity.CanExecuteAsync(activityExecutionContext, cancellationToken))
                    workflowExecutionContext.ScheduleActivity(startActivity, workflow.Input);
            }

            if (workflowExecutionContext.HasScheduledActivities)
            {
                workflow.BlockingActivities.RemoveWhere(startActivityList.Contains);
                workflowExecutionContext.Run();
            }

            return workflowExecutionContext;
        }

        private async Task<IEnumerable<(WorkflowBlueprint, IActivity)>> FilterRunningSingletonsAsync(
            IEnumerable<(WorkflowBlueprint, IActivity)> workflowDefinitions,
            CancellationToken cancellationToken)
        {
            var definitions = workflowDefinitions.ToList();
            var transients = definitions.Where(x => !x.Item1.IsSingleton).ToList();
            var singletons = definitions.Where(x => x.Item1.IsSingleton).ToList();
            var result = transients.ToList();

            foreach (var definition in singletons)
            {
                var instances = await workflowInstanceStore.ListByStatusAsync(
                    definition.Item1.DefinitionId,
                    WorkflowStatus.Running,
                    cancellationToken
                );

                if (!instances.Any())
                {
                    result.Add(definition);
                }
            }

            return result;
        }
    }
}