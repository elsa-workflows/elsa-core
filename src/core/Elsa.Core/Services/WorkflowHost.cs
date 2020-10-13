using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Exceptions;
using Elsa.Expressions;
using Elsa.Messaging.Domain;
using Elsa.Models;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Services
{
    public class WorkflowHost : IWorkflowHost
    {
        private delegate ValueTask<IActivityExecutionResult> ActivityOperation(
            ActivityExecutionContext activityExecutionContext,
            IActivity activity,
            CancellationToken cancellationToken);

        private static readonly ActivityOperation Execute = (context, activity, cancellationToken) =>
            activity.ExecuteAsync(context, cancellationToken);

        private static readonly ActivityOperation Resume = (context, activity, cancellationToken) =>
            activity.ResumeAsync(context, cancellationToken);

        private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
        private readonly IWorkflowInstanceManager _workflowInstanceManager;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowFactory _workflowFactory;
        private readonly IExpressionEvaluator _expressionEvaluator;
        private readonly IMediator _mediator;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public WorkflowHost(
            IWorkflowDefinitionManager workflowDefinitionManager,
            IWorkflowInstanceManager workflowInstanceManager,
            IWorkflowRegistry workflowRegistry,
            IWorkflowFactory workflowFactory,
            IExpressionEvaluator expressionEvaluator,
            IMediator mediator,
            IServiceProvider serviceProvider,
            ILogger<WorkflowHost> logger)
        {
            _workflowDefinitionManager = workflowDefinitionManager;
            _workflowInstanceManager = workflowInstanceManager;
            _workflowRegistry = workflowRegistry;
            _workflowFactory = workflowFactory;
            _expressionEvaluator = expressionEvaluator;
            _mediator = mediator;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async ValueTask<WorkflowInstance> RunWorkflowAsync(
            IWorkflowBlueprint workflowBlueprint,
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _workflowFactory.InstantiateAsync(
                workflowBlueprint,
                correlationId,
                cancellationToken);

            return await RunWorkflowAsync(
                workflowBlueprint,
                workflowInstance,
                activityId,
                input,
                cancellationToken);
        }


        public async ValueTask<WorkflowInstance> RunWorkflowAsync(
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = await _workflowRegistry.GetWorkflowAsync(
                workflowInstance.WorkflowDefinitionId,
                VersionOptions.SpecificVersion(workflowInstance.Version),
                cancellationToken);

            if (workflowBlueprint == null)
                throw new WorkflowException(
                    $"Workflow instance {workflowInstance.Id} references workflow definition {workflowInstance.WorkflowDefinitionId} version {workflowInstance.Version}, but no such workflow definition was found.");

            return await RunWorkflowAsync(workflowBlueprint, workflowInstance, activityId, input, cancellationToken);
        }

        public async ValueTask<WorkflowInstance> RunWorkflowAsync(
            IWorkflowBlueprint workflowBlueprint,
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var workflowExecutionContext = CreateWorkflowExecutionContext(workflowBlueprint, workflowInstance, scope);
            var activity = activityId != null ? workflowBlueprint.GetActivity(activityId) : default;

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

            await _mediator.Publish(new WorkflowExecuted(workflowExecutionContext), cancellationToken);

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
                await _mediator.Publish(statusEvent, cancellationToken);

            workflowExecutionContext.UpdateWorkflowInstance(workflowInstance);
            return workflowInstance;
        }

        private async Task BeginWorkflow(
            WorkflowExecutionContext workflowExecutionContext,
            IActivityBlueprint? activity,
            object? input,
            CancellationToken cancellationToken)
        {
            if (activity == null)
                activity = workflowExecutionContext.WorkflowBlueprint.GetStartActivities().First();

            if (!await CanExecuteAsync(workflowExecutionContext, activity, input, cancellationToken))
                return;

            workflowExecutionContext.Status = WorkflowStatus.Running;
            workflowExecutionContext.ScheduleActivity(activity.Id, input);
            await RunAsync(workflowExecutionContext, Execute, cancellationToken);
        }

        private async Task RunWorkflowAsync(
            WorkflowExecutionContext workflowExecutionContext,
            CancellationToken cancellationToken)
        {
            await RunAsync(workflowExecutionContext, Execute, cancellationToken);
        }

        private async Task ResumeWorkflowAsync(
            WorkflowExecutionContext workflowExecutionContext,
            IActivityBlueprint activityBlueprint,
            object? input,
            CancellationToken cancellationToken)
        {
            if (!await CanExecuteAsync(workflowExecutionContext, activityBlueprint, input, cancellationToken))
                return;

            workflowExecutionContext.BlockingActivities.RemoveWhere(x => x.ActivityId == activityBlueprint.Id);
            workflowExecutionContext.Status = WorkflowStatus.Running;
            workflowExecutionContext.ScheduleActivity(activityBlueprint.Id, input);
            await RunAsync(workflowExecutionContext, Resume, cancellationToken);
        }

        private async ValueTask<bool> CanExecuteAsync(
            WorkflowExecutionContext workflowExecutionContext,
            IActivityBlueprint activityBlueprint,
            object? input,
            CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            var activityExecutionContext = new ActivityExecutionContext(
                workflowExecutionContext,
                scope.ServiceProvider,
                activityBlueprint,
                input);

            var activity = await activityBlueprint.CreateActivityAsync();
            return await activity.CanExecuteAsync(activityExecutionContext, cancellationToken);
        }

        private async ValueTask RunAsync(
            WorkflowExecutionContext workflowExecutionContext,
            ActivityOperation activityOperation,
            CancellationToken cancellationToken = default)
        {
            while (workflowExecutionContext.HasScheduledActivities)
            {
                var scheduledActivity = workflowExecutionContext.PopScheduledActivity();
                var currentActivityId = scheduledActivity.ActivityId;
                var activityBlueprint = workflowExecutionContext.WorkflowBlueprint.GetActivity(currentActivityId)!;

                using (var scope = _serviceProvider.CreateScope())
                {
                    var activityExecutionContext = new ActivityExecutionContext(
                        workflowExecutionContext,
                        scope.ServiceProvider,
                        activityBlueprint,
                        scheduledActivity.Input);

                    var activity = await activityBlueprint.CreateActivityAsync();

                    var result = await activityOperation(activityExecutionContext, activity, cancellationToken);
                    await _mediator.Publish(new ActivityExecuting(activityExecutionContext), cancellationToken);
                    await result.ExecuteAsync(activityExecutionContext, cancellationToken);
                    await _mediator.Publish(new ActivityExecuted(activityExecutionContext), cancellationToken);
                }

                activityOperation = Execute;
                workflowExecutionContext.CompletePass();
            }

            if (workflowExecutionContext.Status == WorkflowStatus.Running)
                workflowExecutionContext.Complete();
        }

        private WorkflowExecutionContext CreateWorkflowExecutionContext(
            IWorkflowBlueprint workflowBlueprint,
            WorkflowInstance workflowInstance,
            IServiceScope serviceScope) =>
            new WorkflowExecutionContext(
                _expressionEvaluator,
                serviceScope.ServiceProvider,
                workflowBlueprint,
                workflowInstance);
    }
}