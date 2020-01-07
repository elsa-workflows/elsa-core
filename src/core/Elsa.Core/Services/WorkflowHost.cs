using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Comparers;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services.Models;
using ScheduledActivity = Elsa.Services.Models.ScheduledActivity;
using WorkflowExecutionScope = Elsa.Models.WorkflowExecutionScope;

namespace Elsa.Services
{
    public class WorkflowHost : IWorkflowHost
    {
        private readonly IScheduler scheduler;
        private readonly IWorkflowRegistry workflowRegistry;
        private readonly IWorkflowInstanceStore workflowInstanceStore;

        public WorkflowHost(
            IScheduler scheduler,
            IWorkflowRegistry workflowRegistry,
            IWorkflowInstanceStore workflowInstanceStore)
        {
            this.scheduler = scheduler;
            this.workflowRegistry = workflowRegistry;
            this.workflowInstanceStore = workflowInstanceStore;
        }
        
        public async Task RunAsync(string id, object? input = default, CancellationToken cancellationToken = default)
        {
            var workflow = await workflowRegistry.GetWorkflowAsync(id, VersionOptions.Published, cancellationToken);
            var workflowExecutionContext = await scheduler.ScheduleActivityAsync(workflow, input, cancellationToken);
            var workflowInstance = CreateWorkflowInstance(workflow, workflowExecutionContext);

            await workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
        }

        public async Task TriggerAsync(string activityType, object? input = default, string? correlationId = default, Func<Variables, bool>? activityStatePredicate = default, CancellationToken cancellationToken = default)
        {
            // Find workflows exposing activities with the specified activity type as workflow triggers.
            
            // Find suspended workflow instances that are blocked on activities with the specified activity type.
            var pendingWorkflowInstances = await workflowInstanceStore.ListByBlockingActivityAsync(activityType, correlationId, cancellationToken);

            if (activityStatePredicate != null)
                pendingWorkflowInstances = pendingWorkflowInstances.Where(x => activityStatePredicate(x.Item2.State));

            foreach (var pendingWorkflowInstance in pendingWorkflowInstances)
            {
                var workflowInstance = pendingWorkflowInstance.Item1;
                var blockingActivity = pendingWorkflowInstance.Item2;
                var workflowExecutionContext = await CreateWorkflowExecutionContextAsync(workflowInstance, cancellationToken);

                workflowExecutionContext = await scheduler.ResumeAsync(workflowExecutionContext, blockingActivity, input, cancellationToken);
                UpdateWorkflowInstance(workflowInstance, workflowExecutionContext);
            }
        }

        private async Task<WorkflowExecutionContext> CreateWorkflowExecutionContextAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
        {
            var workflow = await workflowRegistry.GetWorkflowAsync(workflowInstance.DefinitionId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);
            var activityLookup = workflow.SelectActivities().Distinct().ToDictionary(x => x.Id);
            var scheduledActivities = new Stack<ScheduledActivity>(workflowInstance.ScheduledActivities.Reverse().Select(x => CreateScheduledActivity(x, activityLookup)));
            var blockingActivities = new HashSet<IActivity>(workflowInstance.BlockingActivities.Select(x => workflow.FindActivity(x.ActivityId)));
            var scopes = new Stack<Models.WorkflowExecutionScope>(workflowInstance.Scopes.Reverse().Select(x => CreateWorkflowExecutionScope(x, activityLookup)));
            var status = workflowInstance.Status;
            var persistenceBehavior = workflow.PersistenceBehavior;
            var workflowExecutionContext = scheduler.CreateWorkflowExecutionContext(workflowInstance.Id, scheduledActivities, blockingActivities, scopes, status, persistenceBehavior);

            return workflowExecutionContext;
        }

        private Models.WorkflowExecutionScope CreateWorkflowExecutionScope(WorkflowExecutionScope workflowExecutionScope, IDictionary<string, IActivity> activityLookup)
        {
            var container = workflowExecutionScope.ContainerActivityId != null ? activityLookup[workflowExecutionScope.ContainerActivityId] : default;
            return new Models.WorkflowExecutionScope(container, workflowExecutionScope.Variables);
        }

        private ScheduledActivity CreateScheduledActivity(Elsa.Models.ScheduledActivity scheduledActivityModel, IDictionary<string, IActivity> activityLookup)
        {
            var activity = activityLookup[scheduledActivityModel.ActivityId];
            return new ScheduledActivity(activity, scheduledActivityModel.Input);
        }

        private WorkflowInstance CreateWorkflowInstance(Workflow workflow, WorkflowExecutionContext workflowExecutionContext)
        {
            var workflowInstance = new WorkflowInstance
            {
                Id = workflowExecutionContext.InstanceId,
                Scopes = new Stack<WorkflowExecutionScope>(workflowExecutionContext.Scopes.Select(x => new WorkflowExecutionScope(x.Variables, x.Container.Id))),
                ScheduledActivities = new Stack<Elsa.Models.ScheduledActivity>(workflowExecutionContext.ScheduledActivities.Select(x => new Elsa.Models.ScheduledActivity(x.Activity.Id, x.Input))),
                BlockingActivities = new HashSet<BlockingActivity>(workflowExecutionContext.BlockingActivities.Select(x => new BlockingActivity(x.Id, x.Type)), new BlockingActivityEqualityComparer()),
                Status = workflowExecutionContext.Status,
                Version = workflow.Version,
                CorrelationId = workflowExecutionContext.CorrelationId,
                DefinitionId = workflow.DefinitionId,
            };

            return workflowInstance;
        }
        
        private WorkflowInstance UpdateWorkflowInstance(WorkflowInstance workflowInstance, WorkflowExecutionContext workflowExecutionContext)
        {
            workflowInstance.Scopes = new Stack<WorkflowExecutionScope>(workflowExecutionContext.Scopes.Select(x => new WorkflowExecutionScope(x.Variables, x.Container?.Id)));
            workflowInstance.ScheduledActivities = new Stack<Elsa.Models.ScheduledActivity>(workflowExecutionContext.ScheduledActivities.Select(x => new Elsa.Models.ScheduledActivity(x.Activity.Id, x.Input)));
            workflowInstance.BlockingActivities = new HashSet<BlockingActivity>(workflowExecutionContext.BlockingActivities.Select(x => new BlockingActivity(x.Id, x.Type)), new BlockingActivityEqualityComparer());
            workflowInstance.Status = workflowExecutionContext.Status;
            workflowInstance.CorrelationId = workflowExecutionContext.CorrelationId;

            return workflowInstance;
        }
    }
}