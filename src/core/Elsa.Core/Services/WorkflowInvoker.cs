using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Results;
using Elsa.Services.Extensions;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NodaTime;

namespace Elsa.Services
{
    public class WorkflowInvoker : IWorkflowInvoker
    {
        private readonly IActivityInvoker activityInvoker;
        private readonly IWorkflowFactory workflowFactory;
        private readonly IWorkflowRegistry workflowRegistry;
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly IEnumerable<IWorkflowEventHandler> workflowEventHandlers;
        private readonly IClock clock;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;

        public WorkflowInvoker(
            IActivityInvoker activityInvoker,
            IWorkflowFactory workflowFactory,
            IWorkflowRegistry workflowRegistry,
            IWorkflowInstanceStore workflowInstanceStore,
            IEnumerable<IWorkflowEventHandler> workflowEventHandlers,
            IClock clock,
            IServiceProvider serviceProvider,
            ILogger<WorkflowInvoker> logger)
        {
            this.activityInvoker = activityInvoker;
            this.workflowFactory = workflowFactory;
            this.workflowRegistry = workflowRegistry;
            this.workflowInstanceStore = workflowInstanceStore;
            this.workflowEventHandlers = workflowEventHandlers;
            this.clock = clock;
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }

        public async Task<WorkflowExecutionContext> InvokeAsync(
            Workflow workflow,
            IEnumerable<IActivity> startActivityIds = default,
            CancellationToken cancellationToken = default)
        {
            var workflowExecutionContext = await CreateWorkflowExecutionContextAsync(
                workflow,
                startActivityIds
            );
            await ExecuteWorkflowAsync(workflowExecutionContext, cancellationToken);
            await FinalizeWorkflowExecutionAsync(workflowExecutionContext, cancellationToken);

            return workflowExecutionContext;
        }

        public Task<WorkflowExecutionContext> InvokeAsync(
            WorkflowDefinitionVersion workflowDefinition,
            Variables input = default,
            WorkflowInstance workflowInstance = default,
            IEnumerable<string> startActivityIds = default,
            string correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var workflow = workflowFactory.CreateWorkflow(workflowDefinition, input, workflowInstance);
            var startActivities = workflow.Activities.Find(startActivityIds);

            workflow.CorrelationId = correlationId;
            return InvokeAsync(workflow, startActivities, cancellationToken);
        }

        public Task<WorkflowExecutionContext> InvokeAsync<T>(
            WorkflowInstance workflowInstance = null,
            Variables input = null,
            IEnumerable<string> startActivityIds = default,
            CancellationToken cancellationToken = default) where T : IWorkflow, new()
        {
            var workflow = workflowFactory.CreateWorkflow<T>(input, workflowInstance);
            var startActivities = workflow.Activities.Find(startActivityIds);
            return InvokeAsync(workflow, startActivities, cancellationToken);
        }

        public Task<WorkflowExecutionContext> InvokeAsync(
            WorkflowInstance workflowInstance,
            Variables input = null,
            IEnumerable<string> startActivityIds = default,
            CancellationToken cancellationToken = default)
        {
            var definition = workflowRegistry.GetById(workflowInstance.DefinitionId, workflowInstance.Version);
            return InvokeAsync(
                definition,
                input,
                workflowInstance,
                startActivityIds,
                workflowInstance.CorrelationId,
                cancellationToken
            );
        }

        public async Task<IEnumerable<WorkflowExecutionContext>> TriggerAsync(string activityType,
            Variables input = default,
            string correlationId = default,
            Func<JObject, bool> activityStatePredicate = default,
            CancellationToken cancellationToken = default)
        {
            var workflowInstances = await workflowInstanceStore
                .ListByBlockingActivityAsync(activityType, correlationId, cancellationToken)
                .ToListAsync();

            var workflowDefinitions = workflowRegistry.ListByStartActivity(activityType);

            if (activityStatePredicate != null)
                workflowDefinitions = workflowDefinitions.Where(x => activityStatePredicate(x.Item2.State));

            if (activityStatePredicate != null)
                workflowInstances = workflowInstances.Where(x => activityStatePredicate(x.Item2.State)).ToList();

            workflowDefinitions = await FilterRunningSingletonsAsync(workflowDefinitions, cancellationToken);

            var startedExecutionContexts = await StartWorkflowsAsync(
                workflowDefinitions,
                input,
                correlationId,
                cancellationToken
            );
            var resumedExecutionContexts = await ResumeWorkflowsAsync(
                workflowInstances,
                input,
                correlationId,
                cancellationToken
            );

            return startedExecutionContexts.Concat(resumedExecutionContexts);
        }

        private async Task<IEnumerable<WorkflowExecutionContext>> StartWorkflowsAsync(
            IEnumerable<(WorkflowDefinitionVersion, ActivityDefinition)> workflowDefinitions,
            Variables variables,
            string correlationId,
            CancellationToken cancellationToken1)
        {
            var executionContexts = new List<WorkflowExecutionContext>();

            foreach (var (workflowDefinition, activityDefinition) in workflowDefinitions)
            {
                var startActivities = workflowDefinition.Activities.Where(x => x.Id == activityDefinition.Id)
                    .Select(x => x.Id);
                var executionContext = await InvokeAsync(
                    workflowDefinition,
                    variables,
                    startActivityIds: startActivities,
                    correlationId: correlationId,
                    cancellationToken: cancellationToken1
                );
                executionContexts.Add(executionContext);
            }

            return executionContexts;
        }

        private async Task<IEnumerable<WorkflowExecutionContext>> ResumeWorkflowsAsync(
            IEnumerable<(WorkflowInstance, ActivityInstance)> workflowInstances,
            Variables input,
            string correlationId,
            CancellationToken cancellationToken)
        {
            var executionContexts = new List<WorkflowExecutionContext>();

            foreach (var (workflowInstance, startActivityInstance) in workflowInstances)
            {
                var workflowDefinition = workflowRegistry.GetById(
                    workflowInstance.DefinitionId,
                    workflowInstance.Version
                );

                workflowInstance.Status = WorkflowStatus.Resuming;

                var executionContext = await InvokeAsync(
                    workflowDefinition,
                    input,
                    workflowInstance,
                    new[] { startActivityInstance.Id },
                    correlationId,
                    cancellationToken
                );
                executionContexts.Add(executionContext);
            }

            return executionContexts;
        }

        private async Task ExecuteWorkflowAsync(WorkflowExecutionContext workflowExecutionContext,
            CancellationToken cancellationToken)
        {
            while (workflowExecutionContext.HasScheduledActivities)
            {
                var currentActivity = workflowExecutionContext.PopScheduledActivity();
                var result = await ExecuteActivityAsync(workflowExecutionContext, currentActivity, cancellationToken);

                if (result == null)
                    break;

                await result.ExecuteAsync(this, workflowExecutionContext, cancellationToken);

                workflowExecutionContext.IsFirstPass = false;

                var status = workflowExecutionContext.Workflow.Status;

                if (status == WorkflowStatus.Starting || status == WorkflowStatus.Resuming)
                    workflowExecutionContext.Workflow.Status = WorkflowStatus.Executing;
            }
        }

        private async Task FinalizeWorkflowExecutionAsync(WorkflowExecutionContext workflowExecutionContext,
            CancellationToken cancellationToken)
        {
            // Any other status than Halted means the workflow has ended (because it reached the final activity, was aborted or has faulted).
            if (!workflowExecutionContext.Workflow.IsHalted() && !workflowExecutionContext.Workflow.IsFaulted())
            {
                if (workflowExecutionContext.Workflow.BlockingActivities.Any())
                    workflowExecutionContext.Halt(null);
                else
                    workflowExecutionContext.Finish(clock.GetCurrentInstant());
            }
            else
            {
                if (workflowExecutionContext.HasScheduledHaltingActivities)
                {
                    // Notify event handlers that halting activities are about to be executed.
                    await workflowEventHandlers.InvokeAsync(
                        async x => await x.InvokingHaltedActivitiesAsync(workflowExecutionContext, cancellationToken),
                        logger
                    );

                    // Invoke Halted event on activity drivers that halted the workflow.
                    while (workflowExecutionContext.HasScheduledHaltingActivities)
                    {
                        var currentActivity = workflowExecutionContext.PopScheduledHaltingActivity();
                        var result = await ExecuteActivityHaltedAsync(
                            workflowExecutionContext,
                            currentActivity,
                            cancellationToken
                        );

                        await result.ExecuteAsync(this, workflowExecutionContext, cancellationToken);
                    }
                }
            }

            // Notify event handlers that invocation has ended.
            await workflowEventHandlers.InvokeAsync(
                async x => await x.WorkflowInvokedAsync(workflowExecutionContext, cancellationToken),
                logger
            );
        }

        private async Task<ActivityExecutionResult> ExecuteActivityAsync(WorkflowExecutionContext workflowContext,
            IActivity activity, CancellationToken cancellationToken)
        {
            return await ExecuteActivityAsync(
                workflowContext,
                activity,
                () => activityInvoker.ExecuteAsync(workflowContext, activity, cancellationToken),
                cancellationToken
            );
        }

        private async Task<ActivityExecutionResult> ExecuteActivityAsync(
            WorkflowExecutionContext workflowContext,
            IActivity activity,
            Func<Task<ActivityExecutionResult>> executeAction,
            CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    workflowContext.Workflow.Status = WorkflowStatus.Aborted;
                    workflowContext.Workflow.FinishedAt = clock.GetCurrentInstant();
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

        private async Task<ActivityExecutionResult> ExecuteActivityHaltedAsync(WorkflowExecutionContext workflowContext,
            IActivity activity, CancellationToken cancellationToken)
        {
            return await ExecuteActivityAsync(
                workflowContext,
                activity,
                () => activityInvoker.HaltedAsync(workflowContext, activity, cancellationToken),
                cancellationToken
            );
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
            IEnumerable<IActivity> startActivities)
        {
            var workflowExecutionContext = new WorkflowExecutionContext(workflow, clock, serviceProvider);
            var startActivityList = startActivities?.ToList() ?? workflow.GetStartActivities().Take(1).ToList();

            await workflowExecutionContext.ScheduleActivitiesAsync(startActivityList);

            if (workflowExecutionContext.HasScheduledActivities)
            {
                workflow.BlockingActivities.RemoveWhere(startActivityList.Contains);

                if (workflowExecutionContext.Workflow.Status != WorkflowStatus.Resuming)
                {
                    workflow.Status = WorkflowStatus.Starting;
                    workflow.StartedAt = clock.GetCurrentInstant();
                }
            }

            return workflowExecutionContext;
        }

        private async Task<IEnumerable<(WorkflowDefinitionVersion, ActivityDefinition)>> FilterRunningSingletonsAsync(
            IEnumerable<(WorkflowDefinitionVersion, ActivityDefinition)> workflowDefinitions,
            CancellationToken cancellationToken)
        {
            var result = new List<(WorkflowDefinitionVersion, ActivityDefinition)>();
            
            foreach (var definition in workflowDefinitions)
            {
                var instances = await workflowInstanceStore.ListByStatusAsync(
                    definition.Item1.DefinitionId,
                    WorkflowStatus.Halted,
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