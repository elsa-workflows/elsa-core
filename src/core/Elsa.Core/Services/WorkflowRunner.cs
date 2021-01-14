using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityProviders;
using Elsa.ActivityResults;
using Elsa.Builders;
using Elsa.Events;
using Elsa.Exceptions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services.Models;
using Elsa.Triggers;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;

namespace Elsa.Services
{
    public class WorkflowRunner : IWorkflowRunner
    {
        private delegate ValueTask<IActivityExecutionResult> ActivityOperation(ActivityExecutionContext activityExecutionContext, RuntimeActivityInstance activity);

        private static readonly ActivityOperation Execute = (context, activity) => activity.ActivityType.ExecuteAsync(context);
        private static readonly ActivityOperation Resume = (context, activity) => activity.ActivityType.ResumeAsync(context);

        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowFactory _workflowFactory;
        private readonly IWorkflowSelector _workflowSelector;
        private readonly IWorkflowInstanceStore _workflowInstanceManager;
        private readonly IWorkflowContextManager _workflowContextManager;
        private readonly Func<IWorkflowBuilder> _workflowBuilderFactory;
        private readonly IMediator _mediator;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public WorkflowRunner(
            IWorkflowRegistry workflowRegistry,
            IWorkflowFactory workflowFactory,
            IWorkflowSelector workflowSelector,
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowContextManager workflowContextManager,
            Func<IWorkflowBuilder> workflowBuilderFactory,
            IMediator mediator,
            IServiceProvider serviceProvider,
            ILogger<WorkflowRunner> logger)
        {
            _workflowRegistry = workflowRegistry;
            _workflowFactory = workflowFactory;
            _workflowBuilderFactory = workflowBuilderFactory;
            _mediator = mediator;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _workflowInstanceManager = workflowInstanceStore;
            _workflowContextManager = workflowContextManager;
            _workflowSelector = workflowSelector;
        }

        public async Task TriggerWorkflowsAsync<TTrigger>(
            Func<TTrigger, bool> predicate,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
            where TTrigger : ITrigger
        {
            var results = await _workflowSelector.SelectWorkflowsAsync(predicate, cancellationToken).ToList();

            foreach (var result in results)
            {
                if (result.WorkflowInstanceId != null)
                {
                    var workflowInstance = await _workflowInstanceManager.FindByIdAsync(result.WorkflowInstanceId, cancellationToken);
                    await RunWorkflowAsync(result.WorkflowBlueprint, workflowInstance!, result.ActivityId, input, cancellationToken);
                }
                else
                    await RunWorkflowAsync(result.WorkflowBlueprint, result.ActivityId, input, correlationId, contextId, cancellationToken);

                if (result.Trigger.IsOneOff)
                    await _workflowSelector.RemoveTriggerAsync(result.Trigger, cancellationToken);
            }
        }

        public async ValueTask<WorkflowInstance> RunWorkflowAsync(
            IWorkflowBlueprint workflowBlueprint,
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _workflowFactory.InstantiateAsync(
                workflowBlueprint,
                correlationId,
                contextId,
                cancellationToken);

            return await RunWorkflowAsync(workflowBlueprint, workflowInstance, activityId, input, cancellationToken);
        }

        public async ValueTask<WorkflowInstance> RunWorkflowAsync<T>(
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
            where T : IWorkflow =>
            await RunWorkflowAsync(_workflowBuilderFactory().Build<T>(), activityId, input, correlationId, contextId, cancellationToken);

        public async ValueTask<WorkflowInstance> RunWorkflowAsync<T>(
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default)
            where T : IWorkflow =>
            await RunWorkflowAsync(_workflowBuilderFactory().Build<T>(), workflowInstance, activityId, input, cancellationToken);

        public async ValueTask<WorkflowInstance> RunWorkflowAsync(
            IWorkflow workflow,
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = _workflowBuilderFactory().Build(workflow);
            return await RunWorkflowAsync(workflowBlueprint, activityId, input, correlationId, contextId, cancellationToken);
        }

        public async ValueTask<WorkflowInstance> RunWorkflowAsync(
            IWorkflow workflow,
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = _workflowBuilderFactory().Build(workflow);
            return await RunWorkflowAsync(workflowBlueprint, workflowInstance, activityId, input, cancellationToken);
        }

        public async ValueTask<WorkflowInstance> RunWorkflowAsync(
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = await _workflowRegistry.GetWorkflowAsync(
                workflowInstance.DefinitionId,
                workflowInstance.TenantId,
                VersionOptions.SpecificVersion(workflowInstance.Version),
                cancellationToken);

            if (workflowBlueprint == null) 
                throw new WorkflowException($"Workflow instance {workflowInstance.Id} references workflow definition {workflowInstance.DefinitionId} version {workflowInstance.Version}, but no such workflow definition was found.");

            return await RunWorkflowAsync(workflowBlueprint, workflowInstance, activityId, input, cancellationToken);
        }

        public async ValueTask<WorkflowInstance> RunWorkflowAsync(
            IWorkflowBlueprint workflowBlueprint,
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default)
        {
            using var workflowExecutionScope = _serviceProvider.CreateScope();
            
            var workflowExecutionContext = new WorkflowExecutionContext(workflowExecutionScope, workflowBlueprint, workflowInstance, input);

            if (!string.IsNullOrWhiteSpace(workflowInstance.ContextId))
            {
                var loadContext = new LoadWorkflowContext(workflowExecutionContext);
                workflowExecutionContext.WorkflowContext = await _workflowContextManager.LoadContext(loadContext, cancellationToken);
            }

            await _mediator.Publish(new WorkflowExecuting(workflowExecutionContext), cancellationToken);

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
                    await ResumeWorkflowAsync(workflowExecutionContext, activity!, input, cancellationToken);
                    break;
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
                await _mediator.Publish(statusEvent, cancellationToken);

            await _mediator.Publish(new WorkflowExecutionFinished(workflowExecutionContext), cancellationToken);
            return workflowExecutionContext.WorkflowInstance;
        }

        private async Task BeginWorkflow(WorkflowExecutionContext workflowExecutionContext, IActivityBlueprint? activity, object? input, CancellationToken cancellationToken)
        {
            if (activity == null) 
                activity = workflowExecutionContext.WorkflowBlueprint.GetStartActivities().FirstOrDefault() ?? workflowExecutionContext.WorkflowBlueprint.Activities.First();

            if (!await CanExecuteAsync(workflowExecutionContext, activity, input, cancellationToken))
                return;

            workflowExecutionContext.Begin();
            workflowExecutionContext.ScheduleActivity(activity.Id, input);
            await RunAsync(workflowExecutionContext, Execute, cancellationToken);
        }

        private async Task RunWorkflowAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken) => await RunAsync(workflowExecutionContext, Execute, cancellationToken);

        private async Task ResumeWorkflowAsync(WorkflowExecutionContext workflowExecutionContext, IActivityBlueprint activityBlueprint, object? input, CancellationToken cancellationToken)
        {
            if (!await CanExecuteAsync(workflowExecutionContext, activityBlueprint, input, cancellationToken))
                return;

            var blockingActivities = workflowExecutionContext.WorkflowInstance.BlockingActivities.Where(x => x.ActivityId == activityBlueprint.Id).ToList();

            foreach (var blockingActivity in blockingActivities)
            {
                workflowExecutionContext.WorkflowInstance.BlockingActivities.Remove(blockingActivity);
                await _mediator.Publish(new BlockingActivityRemoved(workflowExecutionContext, blockingActivity), cancellationToken);
            }
            
            workflowExecutionContext.Resume();
            workflowExecutionContext.ScheduleActivity(activityBlueprint.Id, input);
            await RunAsync(workflowExecutionContext, Resume, cancellationToken);
        }

        private async ValueTask<bool> CanExecuteAsync(WorkflowExecutionContext workflowExecutionContext, IActivityBlueprint activityBlueprint, object? input, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            
            var activityExecutionContext = new ActivityExecutionContext(
                scope,
                workflowExecutionContext,
                activityBlueprint,
                input,
                cancellationToken);

            var activity = await activityExecutionContext.ActivateActivityAsync(cancellationToken);
            return await activity.ActivityType.CanExecuteAsync(activityExecutionContext);
        }

        private async ValueTask RunAsync(WorkflowExecutionContext workflowExecutionContext, ActivityOperation activityOperation, CancellationToken cancellationToken = default)
        {
            try
            {
                await RunCoreAsync(workflowExecutionContext, activityOperation, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                workflowExecutionContext.Fault(null, e.Message, e.StackTrace);
            }
        }

        private async ValueTask RunCoreAsync(WorkflowExecutionContext workflowExecutionContext, ActivityOperation activityOperation, CancellationToken cancellationToken = default)
        {
            var scope = workflowExecutionContext.ServiceScope;
            var workflowBlueprint = workflowExecutionContext.WorkflowBlueprint;

            while (workflowExecutionContext.HasScheduledActivities)
            {
                var scheduledActivity = workflowExecutionContext.PopScheduledActivity();
                var currentActivityId = scheduledActivity.ActivityId;
                var activityBlueprint = workflowBlueprint.GetActivity(currentActivityId)!;
                var activityExecutionContext = new ActivityExecutionContext(scope, workflowExecutionContext, activityBlueprint, scheduledActivity.Input, cancellationToken);
                var activity = await activityExecutionContext.ActivateActivityAsync(cancellationToken);
                await _mediator.Publish(new ActivityExecuting(activityExecutionContext), cancellationToken);

                var result = await TryExecuteActivityAsync(activityOperation, activityExecutionContext, activity);
                
                if(result == null)
                    return;

                await _mediator.Publish(new ActivityExecuted(activityExecutionContext), cancellationToken);
                await result.ExecuteAsync(activityExecutionContext, cancellationToken);
                workflowExecutionContext.WorkflowInstance.Output = activityExecutionContext.Output;
                workflowExecutionContext.CompletePass();
                await _mediator.Publish(new WorkflowExecutionPassCompleted(workflowExecutionContext, activityExecutionContext), cancellationToken);

                if (!workflowExecutionContext.HasScheduledActivities) 
                    await _mediator.Publish(new WorkflowExecutionBurstCompleted(workflowExecutionContext, activityExecutionContext), cancellationToken);
                
                activityOperation = Execute;
            }

            if (workflowExecutionContext.HasBlockingActivities)
                workflowExecutionContext.Suspend();

            if (workflowExecutionContext.Status == WorkflowStatus.Running)
                workflowExecutionContext.Complete();
        }

        private async ValueTask<IActivityExecutionResult?> TryExecuteActivityAsync(ActivityOperation activityOperation, ActivityExecutionContext activityExecutionContext, RuntimeActivityInstance activity)
        {
            try
            {
                return await activityOperation(activityExecutionContext, activity);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                activityExecutionContext.WorkflowExecutionContext.Fault(activity.Id, e.Message, e.StackTrace);
            }
            
            return null;
        }
    }
}