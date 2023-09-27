using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Events;
using Elsa.Models;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services.Models;
using Elsa.Services.WorkflowStorage;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Services.Workflows
{
    public class WorkflowRunner : IWorkflowRunner
    {
        private delegate ValueTask<IActivityExecutionResult> ActivityOperation(ActivityExecutionContext activityExecutionContext, IActivity activity);

        private static readonly ActivityOperation Execute = (context, activity) => activity.ExecuteAsync(context);
        private static readonly ActivityOperation Resume = (context, activity) => activity.ResumeAsync(context);

        private readonly IWorkflowContextManager _workflowContextManager;
        private readonly IMediator _mediator;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger _logger;
        private readonly IGetsStartActivities _startingActivitiesProvider;
        private readonly IWorkflowStorageService _workflowStorageService;

        public WorkflowRunner(
            IWorkflowContextManager workflowContextManager,
            IMediator mediator,
            IServiceScopeFactory serviceScopeFactory,
            IGetsStartActivities startingActivitiesProvider,
            IWorkflowStorageService workflowStorageService,
            ILogger<WorkflowRunner> logger)
        {
            _mediator = mediator;
            _serviceScopeFactory = serviceScopeFactory;
            _startingActivitiesProvider = startingActivitiesProvider;
            _workflowStorageService = workflowStorageService;
            _logger = logger;
            _workflowContextManager = workflowContextManager;
        }

        public virtual async Task<RunWorkflowResult> RunWorkflowAsync(
            IWorkflowBlueprint workflowBlueprint,
            WorkflowInstance workflowInstance,
            string? activityId = default,
            CancellationToken cancellationToken = default)
        {
            using var loggingScope = _logger.BeginScope(new WorkflowInstanceLogScope(workflowInstance.Id));
            await using var workflowExecutionScope = _serviceScopeFactory.CreateAsyncScope();

            var input = await _workflowStorageService.LoadAsync(workflowInstance, cancellationToken);

            var workflowExecutionContext = new WorkflowExecutionContext(workflowExecutionScope.ServiceProvider, workflowBlueprint, workflowInstance, input);
            var result = await RunWorkflowInternalAsync(workflowExecutionContext, activityId, cancellationToken);
            await workflowExecutionContext.WorkflowExecutionLog.FlushAsync(cancellationToken);
            return result;
        }

        protected virtual async Task<RunWorkflowResult> RunWorkflowInternalAsync(WorkflowExecutionContext workflowExecutionContext, string? activityId = default, CancellationToken cancellationToken = default)
        {
            var workflowInstance = workflowExecutionContext.WorkflowInstance;

            if (!string.IsNullOrWhiteSpace(workflowInstance.ContextId))
            {
                var loadContext = new LoadWorkflowContext(workflowExecutionContext);
                workflowExecutionContext.WorkflowContext = await _workflowContextManager.LoadContext(loadContext, cancellationToken);
            }

            // If the workflow instance has a CurrentActivity, it means the workflow instance is being retried.
            var currentActivity = workflowInstance.CurrentActivity;

            if (activityId == null && currentActivity != null)
                activityId = currentActivity.ActivityId;

            var workflowBlueprint = workflowExecutionContext.WorkflowBlueprint;
            var activity = activityId != null ? workflowBlueprint.GetActivity(activityId) : default;

            // Give application a chance to prevent workflow from executing.
            var validateWorkflowExecution = new ValidateWorkflowExecution(workflowExecutionContext, activity);
            await _mediator.Publish(validateWorkflowExecution, cancellationToken);

            if (!validateWorkflowExecution.CanExecuteWorkflow)
            {
                _logger.LogInformation("Workflow execution prevented for workflow {WorkflowInstanceId}", workflowInstance.Id);
                return new RunWorkflowResult(workflowInstance, activityId, null, false);
            }

            await _mediator.Publish(new WorkflowExecuting(workflowExecutionContext), cancellationToken);
            RunWorkflowResult runWorkflowResult;

            switch (workflowExecutionContext.Status)
            {
                case WorkflowStatus.Idle:
                    runWorkflowResult = await BeginWorkflow(workflowExecutionContext, activity, cancellationToken);

                    if (!runWorkflowResult.Executed)
                    {
                        if (workflowInstance.WorkflowStatus != WorkflowStatus.Faulted)
                        {
                            _logger.LogDebug("Workflow {WorkflowInstanceId} cannot begin from an idle state (perhaps it needs a specific input)", workflowInstance.Id);
                            return runWorkflowResult;
                        }
                    }

                    break;

                case WorkflowStatus.Running:
                    runWorkflowResult = await RunWorkflowAsync(workflowExecutionContext, activity, cancellationToken);
                    break;

                case WorkflowStatus.Suspended:
                    runWorkflowResult = await ResumeWorkflowAsync(workflowExecutionContext, activity!, cancellationToken);

                    if (!runWorkflowResult.Executed)
                    {
                        if (workflowInstance.WorkflowStatus != WorkflowStatus.Faulted)
                        {
                            _logger.LogDebug("Workflow {WorkflowInstanceId} cannot be resumed from a suspended state (perhaps it needs a specific input)", workflowInstance.Id);
                            return runWorkflowResult;
                        }
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            await _mediator.Publish(new WorkflowExecuted(workflowExecutionContext), cancellationToken);

            var statusEvents = workflowExecutionContext.Status switch
            {
                WorkflowStatus.Cancelled => new INotification[] { new WorkflowCancelled(workflowExecutionContext), new WorkflowInstanceCancelled(workflowInstance) },
                WorkflowStatus.Finished => new INotification[] { new WorkflowCompleted(workflowExecutionContext) },
                WorkflowStatus.Faulted => new INotification[] { new WorkflowFaulted(workflowExecutionContext) },
                WorkflowStatus.Suspended => new INotification[] { new WorkflowSuspended(workflowExecutionContext) },
                _ => Array.Empty<INotification>()
            };

            foreach (var statusEvent in statusEvents)
                await _mediator.Publish(statusEvent, cancellationToken);

            await _mediator.Publish(new WorkflowExecutionFinished(workflowExecutionContext), cancellationToken);
            return runWorkflowResult;
        }

        private async Task<RunWorkflowResult> BeginWorkflow(WorkflowExecutionContext workflowExecutionContext, IActivityBlueprint? activity, CancellationToken cancellationToken)
        {
            if (activity == null)
                activity = _startingActivitiesProvider.GetStartActivities(workflowExecutionContext.WorkflowBlueprint).FirstOrDefault() ?? workflowExecutionContext.WorkflowBlueprint.Activities.FirstOrDefault();

            if (activity == null)
            {
                _logger.LogWarning("Workflow {WorkflowDefinitionId} has no activities", workflowExecutionContext.WorkflowBlueprint.Id);
                return new RunWorkflowResult(workflowExecutionContext.WorkflowInstance, null, null, false);
            }

            try
            {
                if (!await CanExecuteAsync(workflowExecutionContext, activity, false, cancellationToken))
                    return new RunWorkflowResult(workflowExecutionContext.WorkflowInstance, activity.Id, null, false);

                var currentStatus = workflowExecutionContext.WorkflowInstance.WorkflowStatus;
                workflowExecutionContext.Begin();
                workflowExecutionContext.ScheduleActivity(activity.Id);
                await _mediator.Publish(new WorkflowStatusChanged(workflowExecutionContext.WorkflowInstance, workflowExecutionContext.WorkflowInstance.WorkflowStatus, currentStatus), cancellationToken);
                await RunCoreAsync(workflowExecutionContext, Execute, cancellationToken);
                return new RunWorkflowResult(workflowExecutionContext.WorkflowInstance, activity?.Id, null, true);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to run workflow {WorkflowInstanceId}", workflowExecutionContext.WorkflowInstance.Id);
                workflowExecutionContext.Fault(e, workflowExecutionContext.WorkflowInstance.CurrentActivity?.ActivityId, null, false);

                if (workflowExecutionContext.WorkflowInstance.CurrentActivity != null)
                {
                    var currentActivityBlueprint = workflowExecutionContext.WorkflowBlueprint.Activities.First(bp => bp.Id == workflowExecutionContext.WorkflowInstance.CurrentActivity.ActivityId);
                    workflowExecutionContext.AddEntry(currentActivityBlueprint, "Faulted", null, SimpleException.FromException(e));
                }

                return new RunWorkflowResult(workflowExecutionContext.WorkflowInstance, activity?.Id, e, false);
            }
        }

        private async Task<RunWorkflowResult> RunWorkflowAsync(WorkflowExecutionContext workflowExecutionContext, IActivityBlueprint? activity, CancellationToken cancellationToken)
        {
            try
            {
                await RunCoreAsync(workflowExecutionContext, Execute, cancellationToken);
                return new RunWorkflowResult(workflowExecutionContext.WorkflowInstance, activity?.Id, null, true);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to run workflow {WorkflowInstanceId}", workflowExecutionContext.WorkflowInstance.Id);
                workflowExecutionContext.Fault(e, workflowExecutionContext.WorkflowInstance.CurrentActivity?.ActivityId, null, false);

                if (workflowExecutionContext.WorkflowInstance.CurrentActivity != null)
                {
                    var currentActivityBlueprint = workflowExecutionContext.WorkflowBlueprint.Activities.First(bp => bp.Id == workflowExecutionContext.WorkflowInstance.CurrentActivity.ActivityId);
                    workflowExecutionContext.AddEntry(currentActivityBlueprint, "Faulted", null, SimpleException.FromException(e));
                }

                return new RunWorkflowResult(workflowExecutionContext.WorkflowInstance, activity?.Id, e, false);
            }
        }

        private async Task<RunWorkflowResult> ResumeWorkflowAsync(WorkflowExecutionContext workflowExecutionContext, IActivityBlueprint activity, CancellationToken cancellationToken)
        {
            try
            {
                if (!await CanExecuteAsync(workflowExecutionContext, activity, true, cancellationToken))
                    return new RunWorkflowResult(workflowExecutionContext.WorkflowInstance, activity.Id, null, false);

                var blockingActivities = workflowExecutionContext.WorkflowInstance.BlockingActivities.Where(x => x.ActivityId == activity.Id).ToList();

                foreach (var blockingActivity in blockingActivities)
                    await workflowExecutionContext.RemoveBlockingActivityAsync(blockingActivity);

                var currentStatus = workflowExecutionContext.WorkflowInstance.WorkflowStatus;
                workflowExecutionContext.Resume();
                workflowExecutionContext.ScheduleActivity(activity.Id);
                await _mediator.Publish(new WorkflowStatusChanged(workflowExecutionContext.WorkflowInstance, workflowExecutionContext.WorkflowInstance.WorkflowStatus, currentStatus), cancellationToken);
                await RunCoreAsync(workflowExecutionContext, Resume, cancellationToken);
                return new RunWorkflowResult(workflowExecutionContext.WorkflowInstance, activity.Id, null, true);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to run workflow {WorkflowInstanceId}", workflowExecutionContext.WorkflowInstance.Id);
                workflowExecutionContext.Fault(e, workflowExecutionContext.WorkflowInstance.CurrentActivity?.ActivityId, null, false);

                if (workflowExecutionContext.WorkflowInstance.CurrentActivity != null)
                {
                    var currentActivityBlueprint = workflowExecutionContext.WorkflowBlueprint.Activities.First(bp => bp.Id == workflowExecutionContext.WorkflowInstance.CurrentActivity.ActivityId);
                    workflowExecutionContext.AddEntry(currentActivityBlueprint, "Faulted", null, SimpleException.FromException(e));
                }

                return new RunWorkflowResult(workflowExecutionContext.WorkflowInstance, activity.Id, e, false);
            }
        }

        private async ValueTask<bool> CanExecuteAsync(WorkflowExecutionContext workflowExecutionContext, IActivityBlueprint activityBlueprint, bool resuming, CancellationToken cancellationToken)
        {
            var activityExecutionContext = new ActivityExecutionContext(
                workflowExecutionContext.ServiceProvider,
                workflowExecutionContext,
                activityBlueprint,
                workflowExecutionContext.Input,
                resuming,
                cancellationToken);

            using var executionScope = AmbientActivityExecutionContext.EnterScope(activityExecutionContext);
            var runtimeActivityInstance = await activityExecutionContext.ActivateActivityAsync(cancellationToken);
            var activityType = runtimeActivityInstance.ActivityType;
            var activity = await activityType.ActivateAsync(activityExecutionContext);
            var canExecute = await activityType.CanExecuteAsync(activityExecutionContext, activity);

            if (canExecute)
            {
                var canExecuteMessage = new ValidateWorkflowActivityExecution(activityExecutionContext, runtimeActivityInstance);
                await _mediator.Publish(canExecuteMessage, cancellationToken);
                canExecute = canExecuteMessage.CanExecuteActivity;
            }

            return canExecute;
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
                var outputReference = workflowInstance.Output;
                var output = outputReference != null ? await _workflowStorageService.LoadAsync(outputReference.ProviderName, new WorkflowStorageContext(workflowInstance, outputReference.ActivityId), "Output", cancellationToken) : null;
                var input = !burstStarted ? workflowExecutionContext.Input : scheduledActivity.Input ?? output;
                var activityExecutionContext = new ActivityExecutionContext(scope, workflowExecutionContext, activityBlueprint, input, resuming, cancellationToken);
                var isComposite = activityBlueprint is CompositeActivityBlueprint;

                try
                {
                    var runtimeActivityInstance = await activityExecutionContext.ActivateActivityAsync(cancellationToken);
                    var activityType = runtimeActivityInstance.ActivityType;
                    using var executionScope = AmbientActivityExecutionContext.EnterScope(activityExecutionContext);
                    await _mediator.Publish(new ActivityActivating(activityExecutionContext), cancellationToken);
                    var activity = await activityType.ActivateAsync(activityExecutionContext);
                    var compositeScheduledValue = isComposite && (activity is CompositeActivity { IsScheduled: true });

                    if (!burstStarted)
                    {
                        await _mediator.Publish(new WorkflowExecutionBurstStarting(workflowExecutionContext, activityExecutionContext), cancellationToken);
                        burstStarted = true;
                    }

                    if (resuming)
                        await _mediator.Publish(new ActivityResuming(activityExecutionContext, activity), cancellationToken);

                    await CheckIfCompositeEventAsync(isComposite
                        , !compositeScheduledValue
                        , new ActivityExecuting(activityExecutionContext, activity)
                        , _mediator
                        , cancellationToken);

                    var result = await TryExecuteActivityAsync(activityOperation, activityExecutionContext, activity, cancellationToken);

                    await CheckIfCompositeEventAsync(isComposite
                        , compositeScheduledValue
                        , new ActivityExecuted(activityExecutionContext, activity)
                        , _mediator
                        , cancellationToken);

                    await _mediator.Publish(new ActivityExecutionResultExecuting(result, activityExecutionContext), cancellationToken);
                    await result.ExecuteAsync(activityExecutionContext, cancellationToken);

                    workflowExecutionContext.CompletePass();
                    workflowInstance.LastExecutedActivityId = currentActivityId;

                    await CheckIfCompositeEventAsync(isComposite
                        , compositeScheduledValue
                        , new ActivityExecutionResultExecuted(result, activityExecutionContext)
                        , _mediator
                        , cancellationToken);

                    if (workflowExecutionContext.Status == WorkflowStatus.Faulted)
                    {
                        await _mediator.Publish(new WorkflowFaulting(activityExecutionContext, activity), cancellationToken);

                        // Only break out of loop if the status hasn't changed to Cancelled, in which case we might need to continue executing any scheduled compensation activities.
                        if (workflowExecutionContext.Status == WorkflowStatus.Faulted)
                            break;
                    }

                    await _mediator.Publish(new WorkflowExecutionPassCompleted(workflowExecutionContext, activityExecutionContext), cancellationToken);

                    if (!workflowExecutionContext.HasScheduledActivities)
                        await _mediator.Publish(new WorkflowExecutionBurstCompleted(workflowExecutionContext, activityExecutionContext), cancellationToken);

                    activityOperation = Execute;
                }
                catch (Exception e)
                {
                    await _mediator.Publish(new ActivityExecutionFailed(e, activityExecutionContext), cancellationToken);
                    throw;
                }
            }

            workflowInstance.CurrentActivity = null;

            if (workflowExecutionContext.Status != WorkflowStatus.Faulted)
            {
                if (workflowExecutionContext.HasBlockingActivities)
                    workflowExecutionContext.Suspend();
            }

            if (workflowExecutionContext.Status == WorkflowStatus.Running)
                await workflowExecutionContext.CompleteAsync();
        }

        // because Composite Activity doesn't encapsulate child activity
        // but create an activity before and after
        // so we have to get only the begin event of first activity
        // and end event of last activity.
        private async Task CheckIfCompositeEventAsync(bool isComposite, bool scheduleValue, INotification notification, IMediator mediator, CancellationToken cancellationToken)
        {
            if (isComposite)
            {
                if (scheduleValue)
                    await mediator.Publish(notification, cancellationToken);
            }
            else
                await mediator.Publish(notification, cancellationToken);
        }

        private async ValueTask<IActivityExecutionResult> TryExecuteActivityAsync(
            ActivityOperation activityOperation,
            ActivityExecutionContext activityExecutionContext,
            IActivity activity,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await activityOperation(activityExecutionContext, activity);

                if (result is FaultResult faultResult)
                {
                    if (faultResult?.Exception != null)
                        throw faultResult.Exception;

                    throw new Exception(faultResult?.Message);
                }

                return result;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to run activity {ActivityId} of workflow {WorkflowInstanceId}", activity.Id, activityExecutionContext.WorkflowInstance.Id);

                await _mediator.Publish(new ActivityFaulted(e, activityExecutionContext, activity), cancellationToken);

                return new FaultResult(e);
            }
        }
    }
}