using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Exceptions;
using Elsa.Expressions;
using Elsa.Messaging.Domain;
using Elsa.Models;
using Elsa.Queries;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using NodaTime;
using ScheduledActivity = Elsa.Services.Models.ScheduledActivity;

namespace Elsa.Services
{
    public class WorkflowHost : IWorkflowHost
    {
        private delegate Task<IActivityExecutionResult> ActivityOperation(
            ActivityExecutionContext activityExecutionContext,
            IActivity activity,
            CancellationToken cancellationToken);

        private static readonly ActivityOperation Execute = (context, activity, cancellationToken) =>
            activity.ExecuteAsync(context, cancellationToken);

        private static readonly ActivityOperation Resume = (context, activity, cancellationToken) =>
            activity.ResumeAsync(context, cancellationToken);

        private readonly IWorkflowInstanceManager _workflowInstanceManager;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowActivator _workflowActivator;
        private readonly IExpressionEvaluator _expressionEvaluator;
        private readonly IClock _clock;
        private readonly IMediator _mediator;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public WorkflowHost(
            IWorkflowInstanceManager workflowInstanceManager,
            IWorkflowRegistry workflowRegistry,
            IWorkflowActivator workflowActivator,
            IExpressionEvaluator expressionEvaluator,
            IClock clock,
            IMediator mediator,
            IServiceProvider serviceProvider,
            ILogger<WorkflowHost> logger)
        {
            _workflowInstanceManager = workflowInstanceManager;
            _workflowRegistry = workflowRegistry;
            _workflowActivator = workflowActivator;
            _expressionEvaluator = expressionEvaluator;
            _clock = clock;
            _mediator = mediator;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<WorkflowExecutionContext?> RunWorkflowInstanceAsync(
            string workflowInstanceId,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _workflowInstanceManager.GetByWorkflowInstanceIdAsync(workflowInstanceId, cancellationToken);

            if (workflowInstance == null)
            {
                _logger.LogDebug("Workflow instance {WorkflowInstanceId} does not exist.", workflowInstanceId);
                return null;
            }

            return await RunWorkflowInstanceAsync(workflowInstance, activityId, input, cancellationToken);
        }

        public async Task<WorkflowExecutionContext?> RunWorkflowInstanceAsync(
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default)
        {
            var workflow = await _workflowRegistry.GetWorkflowAsync(
                workflowInstance.WorkflowDefinitionId,
                VersionOptions.SpecificVersion(workflowInstance.Version),
                cancellationToken);

            if (workflow == null)
                throw new WorkflowException(
                    $"The specified workflow definition {workflowInstance.WorkflowDefinitionId} is either not registered or not published.");

            return await RunAsync(workflow, workflowInstance, activityId, input, cancellationToken);
        }

        public async Task<WorkflowExecutionContext> RunWorkflowDefinitionAsync(
            string workflowDefinitionId,
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var workflow = await _workflowRegistry.GetWorkflowAsync(
                workflowDefinitionId,
                VersionOptions.Published,
                cancellationToken);

            if (workflow == null)
                throw new WorkflowException(
                    $"The specified workflow definition {workflowDefinitionId} is either not registered or not published.");

            var workflowInstance = await _workflowActivator.ActivateAsync(workflow, correlationId, cancellationToken);
            return await RunAsync(workflow, workflowInstance, activityId, input, cancellationToken);
        }

        public async Task<WorkflowExecutionContext> RunWorkflowAsync(
            Workflow workflow,
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _workflowActivator.ActivateAsync(workflow, correlationId, cancellationToken);
            return await RunAsync(workflow, workflowInstance, activityId, input, cancellationToken);
        }

        private async Task<WorkflowExecutionContext> RunAsync(
            Workflow workflow,
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default)
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

            return workflowExecutionContext;
        }

        private async Task BeginWorkflow(WorkflowExecutionContext workflowExecutionContext,
            IActivity? activity,
            object? input,
            CancellationToken cancellationToken)
        {
            if (activity == null)
                activity = workflowExecutionContext.GetStartActivities().First();

            if (!await CanExecuteAsync(workflowExecutionContext, activity, input, cancellationToken))
                return;

            workflowExecutionContext.Status = WorkflowStatus.Running;
            workflowExecutionContext.ScheduleActivity(activity, input);
            await RunAsync(workflowExecutionContext, Execute, cancellationToken);
        }

        private async Task RunWorkflowAsync(WorkflowExecutionContext workflowExecutionContext,
            CancellationToken cancellationToken)
        {
            await RunAsync(workflowExecutionContext, Execute, cancellationToken);
        }

        private async Task ResumeWorkflowAsync(WorkflowExecutionContext workflowExecutionContext,
            IActivity activity,
            object? input,
            CancellationToken cancellationToken)
        {
            if (!await CanExecuteAsync(workflowExecutionContext, activity, input, cancellationToken))
                return;

            workflowExecutionContext.BlockingActivities.Remove(activity);
            workflowExecutionContext.Status = WorkflowStatus.Running;
            workflowExecutionContext.ScheduleActivity(activity, input);
            await RunAsync(workflowExecutionContext, Resume, cancellationToken);
        }

        private Task<bool> CanExecuteAsync(WorkflowExecutionContext workflowExecutionContext,
            IActivity activity,
            object? input,
            CancellationToken cancellationToken)
        {
            var activityExecutionContext = new ActivityExecutionContext(workflowExecutionContext, activity, input);
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
                var activityExecutionContext = new ActivityExecutionContext(
                    workflowExecutionContext,
                    currentActivity,
                    scheduledActivity.Input);
                await activityExecutionContext.SetActivityPropertiesAsync(cancellationToken);
                var result = await activityOperation(activityExecutionContext, currentActivity, cancellationToken);

                await _mediator.Publish(new ActivityExecuting(activityExecutionContext), cancellationToken);
                await result.ExecuteAsync(activityExecutionContext, cancellationToken);
                await _mediator.Publish(new ActivityExecuted(activityExecutionContext), cancellationToken);

                activityOperation = Execute;
                workflowExecutionContext.CompletePass();
            }

            if (workflowExecutionContext.Status == WorkflowStatus.Running)
                workflowExecutionContext.Complete();
        }

        private ScheduledActivity CreateScheduledActivity(Elsa.Models.ScheduledActivity scheduledActivityModel,
            IDictionary<string, IActivity> activityLookup)
        {
            var activity = activityLookup[scheduledActivityModel.ActivityId];
            return new ScheduledActivity(activity, scheduledActivityModel.Input);
        }

        private WorkflowExecutionContext CreateWorkflowExecutionContext(
            Workflow workflow,
            WorkflowInstance workflowInstance)
        {
            var activityInstanceLookup = workflowInstance.Activities.ToDictionary(x => x.Id);

            foreach (var activity in workflow.Activities)
            {
                if (!activityInstanceLookup.ContainsKey(activity.Id))
                    continue;

                var activityInstance = activityInstanceLookup[activity.Id];
                activity.Output = activityInstance.Output;
            }

            return new WorkflowExecutionContext(
                _expressionEvaluator,
                _clock,
                _serviceProvider,
                workflow,
                workflowInstance);
        }
    }
}