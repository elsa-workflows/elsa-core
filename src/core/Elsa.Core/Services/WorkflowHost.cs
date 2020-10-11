using System;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using YesSql.Data;

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

        public async ValueTask<WorkflowExecutionContext> RunWorkflowDefinitionAsync(
            WorkflowDefinition workflowDefinition,
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _workflowFactory.InstantiateAsync(
                workflowDefinition,
                correlationId,
                cancellationToken);

            return await RunWorkflowInstanceAsync(
                workflowDefinition,
                workflowInstance,
                activityId,
                input,
                cancellationToken);
        }
        
        public async ValueTask<WorkflowExecutionContext> RunWorkflowInstanceAsync(
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default)
        {
            var workflowDefinition = await _workflowDefinitionManager.GetAsync(
                workflowInstance.WorkflowDefinitionId,
                VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);

            if(workflowDefinition == null)
                throw new WorkflowException($"No such workflow with definition {workflowInstance.WorkflowDefinitionId}");
            
            return await RunWorkflowInstanceAsync(
                workflowDefinition,
                workflowInstance,
                activityId,
                input,
                cancellationToken);
        }

        public async ValueTask<WorkflowExecutionContext> RunWorkflowInstanceAsync(
            WorkflowDefinition workflowDefinition,
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default)
        {
            var workflowExecutionContext = CreateWorkflowExecutionContext(workflowDefinition, workflowInstance);
            var activity = activityId != null ? workflowDefinition.GetActivityById(activityId) : default;

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

        private async Task BeginWorkflow(
            WorkflowExecutionContext workflowExecutionContext,
            ActivityDefinition? activity,
            object? input,
            CancellationToken cancellationToken)
        {
            if (activity == null)
                activity = workflowExecutionContext.WorkflowDefinition.GetStartActivities().First();

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

        private async Task ResumeWorkflowAsync(
            WorkflowExecutionContext workflowExecutionContext,
            ActivityDefinition activity,
            object? input,
            CancellationToken cancellationToken)
        {
            if (!await CanExecuteAsync(workflowExecutionContext, activity, input, cancellationToken))
                return;

            workflowExecutionContext.BlockingActivities.RemoveWhere(x => x.ActivityId == activity.Id);
            workflowExecutionContext.Status = WorkflowStatus.Running;
            workflowExecutionContext.ScheduleActivity(activity, input);
            await RunAsync(workflowExecutionContext, Resume, cancellationToken);
        }

        private async ValueTask<bool> CanExecuteAsync(
            WorkflowExecutionContext workflowExecutionContext,
            ActivityDefinition activityDefinition,
            object? input,
            CancellationToken cancellationToken)
        {
            var activityExecutionContext = new ActivityExecutionContext(
                workflowExecutionContext,
                activityDefinition,
                input);

            using var scope = _serviceProvider.CreateScope();
            var activityActivator = scope.ServiceProvider.GetRequiredService<IActivityActivator>();
            var activity = await InstantiateActivityAsync(
                activityActivator,
                activityExecutionContext,
                cancellationToken);
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
                var currentActivity = scheduledActivity.ActivityDefinition;

                var activityExecutionContext = new ActivityExecutionContext(
                    workflowExecutionContext,
                    currentActivity,
                    scheduledActivity.Input);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var activityActivator = scope.ServiceProvider.GetRequiredService<IActivityActivator>();
                    
                    var activity = await InstantiateActivityAsync(
                        activityActivator,
                        activityExecutionContext,
                        cancellationToken);
                    
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
            WorkflowDefinition workflowDefinition,
            WorkflowInstance workflowInstance) =>
            new WorkflowExecutionContext(
                _expressionEvaluator,
                _serviceProvider,
                workflowDefinition,
                workflowInstance);

        private async ValueTask<IActivity> InstantiateActivityAsync(
            IActivityActivator activityActivator,
            ActivityExecutionContext activityExecutionContext,
            CancellationToken cancellationToken)
        {
            var activity = activityActivator.ActivateActivity(activityExecutionContext.ActivityDefinition);
            await activityExecutionContext.SetActivityPropertiesAsync(activity, cancellationToken);
            return activity;
        }
    }
}