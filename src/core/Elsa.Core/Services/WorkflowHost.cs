using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Comparers;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Messaging.Domain;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Results;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using NodaTime;
using ScheduledActivity = Elsa.Services.Models.ScheduledActivity;

namespace Elsa.Services
{
    public class WorkflowHost : IWorkflowHost
    {
        private delegate Task<IActivityExecutionResult> ActivityOperation(ActivityExecutionContext activityExecutionContext, IActivity activity, CancellationToken cancellationToken);

        private static readonly ActivityOperation Execute = (context, activity, cancellationToken) => activity.ExecuteAsync(context, cancellationToken);
        private static readonly ActivityOperation Resume = (context, activity, cancellationToken) => activity.ResumeAsync(context, cancellationToken);

        private readonly IWorkflowRegistry workflowRegistry;
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly IWorkflowActivator workflowActivator;
        private readonly IExpressionEvaluator expressionEvaluator;
        private readonly IClock clock;
        private readonly IMediator mediator;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;

        public WorkflowHost(
            IWorkflowRegistry workflowRegistry,
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowActivator workflowActivator,
            IExpressionEvaluator expressionEvaluator,
            IClock clock,
            IMediator mediator,
            IServiceProvider serviceProvider,
            ILogger<WorkflowHost> logger)
        {
            this.workflowRegistry = workflowRegistry;
            this.workflowInstanceStore = workflowInstanceStore;
            this.workflowActivator = workflowActivator;
            this.expressionEvaluator = expressionEvaluator;
            this.clock = clock;
            this.mediator = mediator;
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }

        public async Task<WorkflowExecutionContext?> RunWorkflowInstanceAsync(string workflowInstanceId, string? activityId = default, object? input = default, CancellationToken cancellationToken = default)
        {
            var workflowInstance = await workflowInstanceStore.GetByIdAsync(workflowInstanceId, cancellationToken);

            if (workflowInstance == null)
            {
                logger.LogDebug("Workflow instance {WorkflowInstanceId} does not exist.", workflowInstanceId);
                return null;
            }

            return await RunWorkflowInstanceAsync(workflowInstance, activityId, input, cancellationToken);
        }

        public async Task<WorkflowExecutionContext?> RunWorkflowInstanceAsync(WorkflowInstance workflowInstance, string? activityId = default, object? input = default, CancellationToken cancellationToken = default)
        {
            var workflow = await workflowRegistry.GetWorkflowAsync(workflowInstance.DefinitionId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);
            return await RunAsync(workflow, workflowInstance, activityId, input, cancellationToken);
        }

        public async Task<WorkflowExecutionContext> RunWorkflowDefinitionAsync(string workflowDefinitionId, string? activityId, object? input = default, string? correlationId = default, CancellationToken cancellationToken = default)
        {
            var workflow = await workflowRegistry.GetWorkflowAsync(workflowDefinitionId, VersionOptions.Published, cancellationToken);
            var workflowInstance = await workflowActivator.ActivateAsync(workflow, correlationId, cancellationToken);

            return await RunAsync(workflow, workflowInstance, activityId, input, cancellationToken);
        }

        public async Task<WorkflowExecutionContext> RunWorkflowAsync(Workflow workflow, string? activityId = default, object? input = default, string? correlationId = default, CancellationToken cancellationToken = default)
        {
            var workflowInstance = await workflowActivator.ActivateAsync(workflow, correlationId, cancellationToken);
            return await RunAsync(workflow, workflowInstance, activityId, input, cancellationToken);
        }

        private async Task<WorkflowExecutionContext> RunAsync(Workflow workflow, WorkflowInstance workflowInstance, string? activityId = default, object? input = default, CancellationToken cancellationToken = default)
        {
            var workflowExecutionContext = CreateWorkflowExecutionContext(workflow, workflowInstance);
            var activity = activityId != null ? workflow.GetActivity(activityId) : default;

            switch (workflowExecutionContext.Status)
            {
                case WorkflowStatus.Idle:
                    await BeginWorkflow(workflowExecutionContext, activity, input, cancellationToken);
                    break;
                case WorkflowStatus.Running:
                    await RunWorkflowAsync(workflowExecutionContext, cancellationToken);
                    break;
                case WorkflowStatus.Suspended:
                    await ResumeWorkflowAsync(workflowExecutionContext, activity, input, cancellationToken);
                    break;
            }

            await mediator.Publish(new WorkflowExecuted(workflowExecutionContext), cancellationToken);

            var statusEvent = default(object);

            switch (workflowExecutionContext.Status)
            {
                case WorkflowStatus.Cancelled:
                    statusEvent = new WorkflowCancelled(workflowExecutionContext);
                    break;
                case WorkflowStatus.Completed:
                    statusEvent = new WorkflowCompleted(workflowExecutionContext);
                    break;
                case WorkflowStatus.Faulted:
                    statusEvent = new WorkflowFaulted(workflowExecutionContext);
                    break;
                case WorkflowStatus.Suspended:
                    statusEvent = new WorkflowSuspended(workflowExecutionContext);
                    break;
            }

            if (statusEvent != null)
                await mediator.Publish(statusEvent, cancellationToken);
            
            return workflowExecutionContext;
        }

        private async Task BeginWorkflow(WorkflowExecutionContext workflowExecutionContext, IActivity? activity, object? input, CancellationToken cancellationToken)
        {
            if (activity == null)
                activity = workflowExecutionContext.GetStartActivities().First();

            if (!await CanExecuteAsync(workflowExecutionContext, activity, input, cancellationToken))
                return;

            workflowExecutionContext.Status = WorkflowStatus.Running;
            workflowExecutionContext.ScheduleActivity(activity, input);
            await RunAsync(workflowExecutionContext, Execute, cancellationToken);
        }

        private async Task RunWorkflowAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            await RunAsync(workflowExecutionContext, Execute, cancellationToken);
        }

        private async Task ResumeWorkflowAsync(WorkflowExecutionContext workflowExecutionContext, IActivity activity, object? input, CancellationToken cancellationToken)
        {
            if (!await CanExecuteAsync(workflowExecutionContext, activity, input, cancellationToken))
                return;
            
            workflowExecutionContext.BlockingActivities.Remove(activity);
            workflowExecutionContext.Status = WorkflowStatus.Running;
            workflowExecutionContext.ScheduleActivity(activity, input);
            await RunAsync(workflowExecutionContext, Resume, cancellationToken);
        }
        
        private Task<bool> CanExecuteAsync(WorkflowExecutionContext workflowExecutionContext, IActivity activity, object? input, CancellationToken cancellationToken)
        {
            var activityExecutionContext = new ActivityExecutionContext(workflowExecutionContext, activity, Variable.From(input));
            return activity.CanExecuteAsync(activityExecutionContext, cancellationToken);
        }

        private async Task RunAsync(
            WorkflowExecutionContext workflowExecutionContext,
            ActivityOperation activityOperation,
            CancellationToken cancellationToken = default)
        {
            while (workflowExecutionContext.HasScheduledActivities)
            {
                var scheduledActivity = workflowExecutionContext.PopScheduledActivity();
                var currentActivity = scheduledActivity.Activity;
                var activityExecutionContext = new ActivityExecutionContext(workflowExecutionContext, currentActivity, scheduledActivity.Input);
                var result = await activityOperation(activityExecutionContext, currentActivity, cancellationToken);

                await mediator.Publish(new ActivityExecuting(activityExecutionContext), cancellationToken);
                await result.ExecuteAsync(activityExecutionContext, cancellationToken);
                await mediator.Publish(new ActivityExecuted(activityExecutionContext), cancellationToken);

                activityOperation = Execute;
            }

            if (workflowExecutionContext.Status == WorkflowStatus.Running)
                workflowExecutionContext.Complete();
        }

        private ScheduledActivity CreateScheduledActivity(Elsa.Models.ScheduledActivity scheduledActivityModel, IDictionary<string, IActivity> activityLookup)
        {
            var activity = activityLookup[scheduledActivityModel.ActivityId];
            return new ScheduledActivity(activity, scheduledActivityModel.Input);
        }

        private WorkflowExecutionContext CreateWorkflowExecutionContext(Workflow workflow, WorkflowInstance workflowInstance)
        {
            var activityLookup = workflow.Activities.ToDictionary(x => x.Id);
            var activityInstanceLookup = workflowInstance.Activities.ToDictionary(x => x.Id);
            var scheduledActivities = new Stack<ScheduledActivity>(workflowInstance.ScheduledActivities.Reverse().Select(x => CreateScheduledActivity(x, activityLookup)));
            var blockingActivities = new HashSet<IActivity>(workflowInstance.BlockingActivities.Select(x => activityLookup[x.ActivityId]));
            var variables = workflowInstance.Variables;
            var status = workflowInstance.Status;
            var persistenceBehavior = workflow.PersistenceBehavior;

            foreach (var activity in workflow.Activities)
            {
                if (!activityInstanceLookup.ContainsKey(activity.Id))
                    continue;

                var activityInstance = activityInstanceLookup[activity.Id];
                activity.State = activityInstance.State;
                activity.Output = activityInstance.Output;
            }

            return CreateWorkflowExecutionContext(
                workflowInstance.Id,
                workflow.DefinitionId,
                workflow.Version,
                workflow.Activities,
                workflow.Connections,
                scheduledActivities,
                blockingActivities,
                workflowInstance.CorrelationId,
                variables,
                status,
                persistenceBehavior);
        }

        private WorkflowExecutionContext CreateWorkflowExecutionContext(
            string workflowInstanceId,
            string workflowDefinitionId,
            int version,
            IEnumerable<IActivity> activities,
            IEnumerable<Connection> connections,
            IEnumerable<ScheduledActivity>? scheduledActivities = default,
            IEnumerable<IActivity>? blockingActivities = default,
            string? correlationId = default,
            Variables? variables = default,
            WorkflowStatus status = WorkflowStatus.Running,
            WorkflowPersistenceBehavior persistenceBehavior = WorkflowPersistenceBehavior.WorkflowExecuted)
            => new WorkflowExecutionContext(
                expressionEvaluator,
                clock,
                serviceProvider,
                workflowDefinitionId,
                workflowInstanceId,
                version,
                activities,
                connections,
                scheduledActivities,
                blockingActivities,
                correlationId,
                variables,
                status,
                persistenceBehavior);
    }
}