using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Comparers;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Messages;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Results;
using Elsa.Services.Models;
using MediatR;
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
        private readonly IExpressionEvaluator expressionEvaluator;
        private readonly IIdGenerator idGenerator;
        private readonly IMediator mediator;
        private readonly IServiceProvider serviceProvider;

        public WorkflowHost(
            IWorkflowRegistry workflowRegistry,
            IWorkflowInstanceStore workflowInstanceStore,
            IExpressionEvaluator expressionEvaluator,
            IIdGenerator idGenerator,
            IMediator mediator,
            IServiceProvider serviceProvider)
        {
            this.workflowRegistry = workflowRegistry;
            this.workflowInstanceStore = workflowInstanceStore;
            this.expressionEvaluator = expressionEvaluator;
            this.idGenerator = idGenerator;
            this.mediator = mediator;
            this.serviceProvider = serviceProvider;
        }

        public async Task RunAsync(string workflowInstanceId, string? activityId = default, object? input = default, CancellationToken cancellationToken = default)
        {
            var workflowInstance = await workflowInstanceStore.GetByIdAsync(workflowInstanceId, cancellationToken);
            var workflow = await workflowRegistry.GetWorkflowAsync(workflowInstance.DefinitionId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);
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
        }

        private async Task BeginWorkflow(WorkflowExecutionContext workflowExecutionContext, IActivity activity, object? input, CancellationToken cancellationToken)
        {
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
            workflowExecutionContext.BlockingActivities.Remove(activity);
            workflowExecutionContext.Status = WorkflowStatus.Running;
            workflowExecutionContext.ScheduleActivity(activity, input);
            await RunAsync(workflowExecutionContext, Resume, cancellationToken);
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

                await result.ExecuteAsync(workflowExecutionContext, activityExecutionContext, cancellationToken);
                await mediator.Publish(new ActivityExecuted(workflowExecutionContext, activityExecutionContext), cancellationToken);

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
            var scheduledActivities = new Stack<ScheduledActivity>(workflowInstance.ScheduledActivities.Reverse().Select(x => CreateScheduledActivity(x, activityLookup)));
            var blockingActivities = new HashSet<IActivity>(workflowInstance.BlockingActivities.Select(x => activityLookup[x.ActivityId]));
            var variables = workflowInstance.Variables;
            var status = workflowInstance.Status;
            var persistenceBehavior = workflow.PersistenceBehavior;
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
            string correlationId = default,
            Variables? variables = default,
            WorkflowStatus status = WorkflowStatus.Running,
            WorkflowPersistenceBehavior persistenceBehavior = WorkflowPersistenceBehavior.WorkflowExecuted)
            => new WorkflowExecutionContext(
                expressionEvaluator,
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