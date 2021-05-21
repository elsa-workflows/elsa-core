using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Events;
using Elsa.Models;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Services
{
    public class WorkflowRunner : IWorkflowRunner
    {
        private delegate ValueTask<IActivityExecutionResult> ActivityOperation(ActivityExecutionContext activityExecutionContext, RuntimeActivityInstance activity);

        private static readonly ActivityOperation Execute = (context, activity) => activity.ActivityType.ExecuteAsync(context);
        private static readonly ActivityOperation Resume = (context, activity) => activity.ActivityType.ResumeAsync(context);

        private readonly IWorkflowContextManager _workflowContextManager;
        private readonly IMediator _mediator;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger _logger;
        private readonly IGetsStartActivities _startingActivitiesProvider;

        public WorkflowRunner(
            IWorkflowContextManager workflowContextManager,
            IMediator mediator,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<WorkflowRunner> logger,
            IGetsStartActivities startingActivitiesProvider)
        {
            _mediator = mediator;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _startingActivitiesProvider = startingActivitiesProvider ?? throw new ArgumentNullException(nameof(startingActivitiesProvider));
            _workflowContextManager = workflowContextManager;
        }

        public virtual async Task<RunWorkflowResult> RunWorkflowAsync(
            IWorkflowBlueprint workflowBlueprint,
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default)
        {
            using var loggingScope = _logger.BeginScope(new Dictionary<string, object> { ["WorkflowInstanceId"] = workflowInstance.Id });
            using var workflowExecutionScope = _serviceScopeFactory.CreateScope();
            var workflowExecutionContext = new WorkflowExecutionContext(workflowExecutionScope.ServiceProvider, workflowBlueprint, workflowInstance, input);

            if (!string.IsNullOrWhiteSpace(workflowInstance.ContextId))
            {
                var loadContext = new LoadWorkflowContext(workflowExecutionContext);
                workflowExecutionContext.WorkflowContext = await _workflowContextManager.LoadContext(loadContext, cancellationToken);
            }

            // If the workflow instance has a CurrentActivity, it means the workflow instance is being retried.
            var currentActivity = workflowInstance.CurrentActivity;

            if (currentActivity != null)
            {
                activityId = currentActivity.ActivityId;
                input ??= currentActivity.Input;
            }

            var activity = activityId != null ? workflowBlueprint.GetActivity(activityId) : default;

            // Give application a chance to prevent workflow from executing.
            var validateWorkflowExecution = new ValidateWorkflowExecution(workflowExecutionContext, activity);
            await _mediator.Publish(validateWorkflowExecution, cancellationToken);

            if (!validateWorkflowExecution.CanExecuteWorkflow)
            {
                _logger.LogInformation("Workflow execution prevented for workflow {WorkflowInstanceId}", workflowInstance.Id);
                return new RunWorkflowResult(workflowInstance, activityId, false);
            }

            await _mediator.Publish(new WorkflowExecuting(workflowExecutionContext), cancellationToken);
            RunWorkflowResult runWorkflowResult;

            switch (workflowExecutionContext.Status)
            {
                case WorkflowStatus.Idle:
                    runWorkflowResult = await BeginWorkflow(workflowExecutionContext, activity, input, cancellationToken);

                    if (!runWorkflowResult.Executed)
                    {
                        _logger.LogDebug("Workflow {WorkflowInstanceId} cannot begin from an idle state (perhaps it needs a specific input)", workflowInstance.Id);
                        return runWorkflowResult;
                    }

                    break;

                case WorkflowStatus.Running:
                    await RunWorkflowAsync(workflowExecutionContext, cancellationToken);
                    runWorkflowResult = new RunWorkflowResult(workflowInstance, activityId, true);
                    break;

                case WorkflowStatus.Suspended:
                    runWorkflowResult = await ResumeWorkflowAsync(workflowExecutionContext, activity!, input, cancellationToken);
                    
                    if(!runWorkflowResult.Executed)
                    {
                        _logger.LogDebug("Workflow {WorkflowInstanceId} cannot be resumed from a suspended state (perhaps it needs a specific input)", workflowInstance.Id);
                        return runWorkflowResult;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            await _mediator.Publish(new WorkflowExecuted(workflowExecutionContext), cancellationToken);

            var statusEvent = workflowExecutionContext.Status switch
            {
                WorkflowStatus.Cancelled => new WorkflowCancelled(workflowExecutionContext),
                WorkflowStatus.Finished => new WorkflowCompleted(workflowExecutionContext),
                WorkflowStatus.Faulted => new WorkflowFaulted(workflowExecutionContext),
                WorkflowStatus.Suspended => new WorkflowSuspended(workflowExecutionContext),
                _ => default(INotification)
            };

            if (statusEvent != null)
            {
                _logger.LogTrace("Publishing a status event of type {EventType} for workflow {WorkflowInstanceId}", statusEvent.GetType().Name, workflowInstance.Id);
                await _mediator.Publish(statusEvent, cancellationToken);
            }

            await _mediator.Publish(new WorkflowExecutionFinished(workflowExecutionContext), cancellationToken);
            return runWorkflowResult;
        }

        private async Task<RunWorkflowResult> BeginWorkflow(WorkflowExecutionContext workflowExecutionContext, IActivityBlueprint? activity, object? input, CancellationToken cancellationToken)
        {
            if (activity == null)
                activity = _startingActivitiesProvider.GetStartActivities(workflowExecutionContext.WorkflowBlueprint).FirstOrDefault() ?? workflowExecutionContext.WorkflowBlueprint.Activities.First();

            if (!await CanExecuteAsync(workflowExecutionContext, activity, input, false, cancellationToken))
                return new RunWorkflowResult(workflowExecutionContext.WorkflowInstance, activity.Id, false);

            workflowExecutionContext.Begin();
            workflowExecutionContext.ScheduleActivity(activity.Id, input);
            await RunAsync(workflowExecutionContext, Execute, cancellationToken);
            return new RunWorkflowResult(workflowExecutionContext.WorkflowInstance, activity.Id, true);
        }

        private async Task RunWorkflowAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            await RunAsync(workflowExecutionContext, Execute, cancellationToken);
        }

        private async Task<RunWorkflowResult> ResumeWorkflowAsync(WorkflowExecutionContext workflowExecutionContext, IActivityBlueprint activityBlueprint, object? input, CancellationToken cancellationToken)
        {
            if (!await CanExecuteAsync(workflowExecutionContext, activityBlueprint, input, true, cancellationToken))
                return new RunWorkflowResult(workflowExecutionContext.WorkflowInstance, activityBlueprint.Id, false);

            var blockingActivities = workflowExecutionContext.WorkflowInstance.BlockingActivities.Where(x => x.ActivityId == activityBlueprint.Id).ToList();

            foreach (var blockingActivity in blockingActivities)
            {
                workflowExecutionContext.WorkflowInstance.BlockingActivities.Remove(blockingActivity);
                await _mediator.Publish(new BlockingActivityRemoved(workflowExecutionContext, blockingActivity), cancellationToken);
            }

            workflowExecutionContext.Resume();
            workflowExecutionContext.ScheduleActivity(activityBlueprint.Id, input);
            await RunAsync(workflowExecutionContext, Resume, cancellationToken);
            return new RunWorkflowResult(workflowExecutionContext.WorkflowInstance, activityBlueprint.Id, true);
        }

        private async ValueTask<bool> CanExecuteAsync(WorkflowExecutionContext workflowExecutionContext, IActivityBlueprint activityBlueprint, object? input, bool resuming, CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var activityExecutionContext = new ActivityExecutionContext(
                scope.ServiceProvider,
                workflowExecutionContext,
                activityBlueprint,
                input,
                resuming,
                cancellationToken);

            using var executionScope = AmbientActivityExecutionContext.EnterScope(activityExecutionContext);
            var activity = await activityExecutionContext.ActivateActivityAsync(cancellationToken);
            var canExecute = await activity.ActivityType.CanExecuteAsync(activityExecutionContext);

            if (canExecute)
            {
                var canExecuteMessage = new ValidateWorkflowActivityExecution(activityExecutionContext, activity);
                await _mediator.Publish(canExecuteMessage, cancellationToken);
                canExecute = canExecuteMessage.CanExecuteActivity;
            }

            return canExecute;
        }

        private async ValueTask RunAsync(WorkflowExecutionContext workflowExecutionContext, ActivityOperation activityOperation, CancellationToken cancellationToken = default)
        {
            try
            {
                await RunCoreAsync(workflowExecutionContext, activityOperation, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to run workflow {WorkflowInstanceId}", workflowExecutionContext.WorkflowInstance.Id);
                workflowExecutionContext.Fault(e, null, null, activityOperation == Resume);
            }
        }

        private async ValueTask RunCoreAsync(WorkflowExecutionContext workflowExecutionContext, ActivityOperation activityOperation, CancellationToken cancellationToken = default)
        {
            var scope = workflowExecutionContext.ServiceProvider;
            var workflowBlueprint = workflowExecutionContext.WorkflowBlueprint;
            var workflowInstance = workflowExecutionContext.WorkflowInstance;
            var burstStarted = false;

            while (workflowExecutionContext.HasScheduledActivities)
            {
                var scheduledActivity = workflowInstance.CurrentActivity = workflowExecutionContext.PopScheduledActivity();
                var currentActivityId = scheduledActivity.ActivityId;
                var activityBlueprint = workflowBlueprint.GetActivity(currentActivityId)!;
                var resuming = activityOperation == Resume;
                var activityExecutionContext = new ActivityExecutionContext(scope, workflowExecutionContext, activityBlueprint, scheduledActivity.Input, resuming, cancellationToken);
                var activity = await activityExecutionContext.ActivateActivityAsync(cancellationToken);

                using var executionScope = AmbientActivityExecutionContext.EnterScope(activityExecutionContext);

                if (!burstStarted)
                {
                    await _mediator.Publish(new WorkflowExecutionBurstStarting(workflowExecutionContext, activityExecutionContext), cancellationToken);
                    burstStarted = true;
                }

                if (resuming)
                    await _mediator.Publish(new ActivityResuming(activityExecutionContext), cancellationToken);

                await _mediator.Publish(new ActivityExecuting(resuming, activityExecutionContext), cancellationToken);
                var result = await TryExecuteActivityAsync(activityOperation, activityExecutionContext, activity, cancellationToken);

                if (result == null)
                    return;

                await _mediator.Publish(new ActivityExecuted(resuming, activityExecutionContext), cancellationToken);
                await _mediator.Publish(new ActivityExecutionResultExecuting(result, activityExecutionContext), cancellationToken);
                await result.ExecuteAsync(activityExecutionContext, cancellationToken);
                workflowExecutionContext.WorkflowInstance.Output = activityExecutionContext.Output;
                workflowExecutionContext.CompletePass();
                await _mediator.Publish(new ActivityExecutionResultExecuted(result, activityExecutionContext), cancellationToken);
                await _mediator.Publish(new WorkflowExecutionPassCompleted(workflowExecutionContext, activityExecutionContext), cancellationToken);

                if (!workflowExecutionContext.HasScheduledActivities)
                    await _mediator.Publish(new WorkflowExecutionBurstCompleted(workflowExecutionContext, activityExecutionContext), cancellationToken);

                activityOperation = Execute;
            }

            workflowInstance.CurrentActivity = null;

            if (workflowExecutionContext.HasBlockingActivities)
                workflowExecutionContext.Suspend();

            if (workflowExecutionContext.Status == WorkflowStatus.Running)
                await workflowExecutionContext.CompleteAsync();
        }

        private async ValueTask<IActivityExecutionResult?> TryExecuteActivityAsync(
            ActivityOperation activityOperation,
            ActivityExecutionContext activityExecutionContext,
            RuntimeActivityInstance activity,
            CancellationToken cancellationToken)
        {
            try
            {
                return await activityOperation(activityExecutionContext, activity);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to run activity {ActivityId} of workflow {WorkflowInstanceId}", activity.Id, activityExecutionContext.WorkflowInstance.Id);
                activityExecutionContext.Fault(e);
                await _mediator.Publish(new ActivityFaulted(e, activityExecutionContext), cancellationToken);
            }

            return null;
        }
    }
}