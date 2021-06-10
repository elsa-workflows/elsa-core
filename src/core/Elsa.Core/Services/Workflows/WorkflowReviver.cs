using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services.Dispatch;
using Elsa.Services.Models;

namespace Elsa.Services.Workflows
{
    public class WorkflowReviver : IWorkflowReviver
    {
        private readonly IResumesWorkflow _resumesWorkflow;
        private readonly IWorkflowInstanceDispatcher _workflowInstanceDispatcher;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IGetsStartActivities _startingActivitiesProvider;

        public WorkflowReviver(
            IResumesWorkflow resumesWorkflow,
            IWorkflowInstanceDispatcher workflowInstanceDispatcher,
            IWorkflowRegistry workflowRegistry,
            IWorkflowInstanceStore workflowInstanceStore,
            IGetsStartActivities startingActivitiesProvider)
        {
            _resumesWorkflow = resumesWorkflow;
            _workflowInstanceDispatcher = workflowInstanceDispatcher;
            _workflowRegistry = workflowRegistry;
            _workflowInstanceStore = workflowInstanceStore;
            _startingActivitiesProvider = startingActivitiesProvider ?? throw new ArgumentNullException(nameof(startingActivitiesProvider));
        }

        public async Task<WorkflowInstance> ReviveAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
        {
            if (workflowInstance.WorkflowStatus != WorkflowStatus.Faulted)
                throw new InvalidOperationException($"Cannot revive non-faulted workflow with status {workflowInstance.WorkflowStatus}");

            var fault = workflowInstance.Fault;

            if (fault == null)
                throw new WorkflowException("Cannot revive a workflow with no fault");

            var faultedActivityId = fault.FaultedActivityId;

            if (faultedActivityId == null)
            {
                // The workflow failed before or after running an activity.
                // If no activities were scheduled, the workflow faulted before finishing executing the first activity.
                var hasScheduledActivities = workflowInstance.CurrentActivity != null;
                workflowInstance.WorkflowStatus = !hasScheduledActivities ? WorkflowStatus.Idle : fault.Resuming ? WorkflowStatus.Suspended : WorkflowStatus.Running;
            }
            else
            {
                // An activity caused the fault.
                workflowInstance.WorkflowStatus = fault.Resuming ? WorkflowStatus.Suspended : WorkflowStatus.Running;
                workflowInstance.ScheduledActivities.Push(new ScheduledActivity(faultedActivityId));
            }

            workflowInstance.Fault = null;
            workflowInstance.FaultedAt = null;
            await _workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
            return workflowInstance;
        }

        public async Task<RunWorkflowResult> ReviveAndRunAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
        {
            workflowInstance = await ReviveAsync(workflowInstance, cancellationToken);
            return await _resumesWorkflow.ResumeWorkflowAsync(workflowInstance, null, null, cancellationToken);
        }

        public async Task<WorkflowInstance> ReviveAndQueueAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
        {
            workflowInstance = await ReviveAsync(workflowInstance, cancellationToken);
            var currentActivity = await GetActivityToScheduleAsync(workflowInstance, cancellationToken);
            await _workflowInstanceDispatcher.DispatchAsync(new ExecuteWorkflowInstanceRequest(workflowInstance.Id, currentActivity.ActivityId), cancellationToken);
            return workflowInstance;
        }

        private async Task<ScheduledActivity> GetActivityToScheduleAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
        {
            var currentActivity = workflowInstance.CurrentActivity;

            if (currentActivity != null)
                return currentActivity;

            var workflowBlueprint = await _workflowRegistry.GetWorkflowAsync(workflowInstance.DefinitionId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);

            if (workflowBlueprint == null)
                throw new WorkflowException($"Could not find associated workflow definition {workflowInstance.DefinitionId} with version {workflowInstance.Version}");

            var startActivity = _startingActivitiesProvider.GetStartActivities(workflowBlueprint).FirstOrDefault();

            if (startActivity == null)
                throw new WorkflowException($"Cannot revive workflow {workflowInstance.Id} because it has no start activities");

            return new ScheduledActivity(startActivity.Id);
        }
    }
}