using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Messages;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services.Models;
using MediatR;
using ScheduledActivity = Elsa.Services.Models.ScheduledActivity;
using WorkflowExecutionScope = Elsa.Services.Models.WorkflowExecutionScope;

namespace Elsa.Services
{
    public class Scheduler : IScheduler
    {
        private delegate Task<IActivityExecutionResult> ActivityOperation(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, IActivity activity, CancellationToken cancellationToken);

        private static readonly ActivityOperation Execute = (workflowExecutionContext, activityExecutionContext, activity, cancellationToken) => activity.ExecuteAsync(workflowExecutionContext, activityExecutionContext, cancellationToken);
        private static readonly ActivityOperation Resume = (workflowExecutionContext, activityExecutionContext, activity, cancellationToken) => activity.ResumeAsync(workflowExecutionContext, activityExecutionContext, cancellationToken);

        private readonly IServiceProvider serviceProvider;
        private readonly IExpressionEvaluator expressionEvaluator;
        private readonly IIdGenerator idGenerator;
        private readonly IMediator mediator;

        public Scheduler(
            IExpressionEvaluator expressionEvaluator,
            IIdGenerator idGenerator,
            IMediator mediator,
            IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.expressionEvaluator = expressionEvaluator;
            this.idGenerator = idGenerator;
            this.mediator = mediator;
        }

        public WorkflowExecutionContext CreateWorkflowExecutionContext(
            string workflowInstanceId,
            IEnumerable<ScheduledActivity>? scheduledActivities = default,
            IEnumerable<IActivity>? blockingActivities = default,
            IEnumerable<WorkflowExecutionScope>? scopes = default,
            WorkflowStatus status = WorkflowStatus.Running,
            WorkflowPersistenceBehavior persistenceBehavior = WorkflowPersistenceBehavior.WorkflowExecuted)
            => new WorkflowExecutionContext(expressionEvaluator, serviceProvider, workflowInstanceId, scheduledActivities, blockingActivities, scopes, status, persistenceBehavior);

        public async Task<WorkflowExecutionContext> ScheduleActivityAsync(IActivity activity, object? input = default, CancellationToken cancellationToken = default)
        {
            var instanceId = idGenerator.Generate();
            var workflowExecutionContext = CreateWorkflowExecutionContext(instanceId);

            workflowExecutionContext.ScheduleActivity(activity, input);

            await RunAsync(workflowExecutionContext, cancellationToken);

            return workflowExecutionContext;
        }

        public async Task<WorkflowExecutionContext> ResumeAsync(WorkflowExecutionContext workflowExecutionContext, IActivity blockingActivity, object? input, CancellationToken cancellationToken = default)
        {
            workflowExecutionContext.BlockingActivities.Remove(blockingActivity);
            workflowExecutionContext.ScheduleActivity(blockingActivity, input);
            workflowExecutionContext.Status = WorkflowStatus.Running;
            return await RunAsync(workflowExecutionContext, Resume, cancellationToken);
        }

        public Task<WorkflowExecutionContext> RunAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default)
            => RunAsync(workflowExecutionContext, Execute, cancellationToken);

        private async Task<WorkflowExecutionContext> RunAsync(
            WorkflowExecutionContext workflowExecutionContext,
            ActivityOperation activityOperation,
            CancellationToken cancellationToken = default)
        {
            while (workflowExecutionContext.HasScheduledActivities)
            {
                var scheduledActivity = workflowExecutionContext.PopScheduledActivity();
                var currentActivity = scheduledActivity.Activity;
                var activityExecutionContext = new ActivityExecutionContext(currentActivity, scheduledActivity.Input);
                var result = await activityOperation(workflowExecutionContext, activityExecutionContext, currentActivity, cancellationToken);

                await result.ExecuteAsync(workflowExecutionContext, activityExecutionContext, cancellationToken);

                WorkflowExecutionScope? currentScope;
                
                do
                {
                    currentScope = workflowExecutionContext.CurrentScope;
                    
                    if (currentScope != null && currentActivity != currentScope.Container)
                    {
                        var containerResult = await currentScope.Container.ChildActivityExecutedAsync(workflowExecutionContext, activityExecutionContext, cancellationToken);
                        await containerResult.ExecuteAsync(workflowExecutionContext, activityExecutionContext, cancellationToken);
                    }
                } while (currentScope != workflowExecutionContext.CurrentScope);

                await mediator.Publish(new ActivityExecuted(workflowExecutionContext, activityExecutionContext), cancellationToken);

                activityOperation = Execute;
            }

            if (workflowExecutionContext.Status == WorkflowStatus.Running)
                workflowExecutionContext.Complete();

            return workflowExecutionContext;
        }
    }
}