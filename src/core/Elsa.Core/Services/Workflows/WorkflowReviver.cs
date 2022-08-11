using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Models;
using Elsa.Persistence;
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

            var fault = workflowInstance.Faults;
            if (fault.Count == 0)
                throw new WorkflowException("Cannot revive a workflow with no fault");


            if (fault.Count > 0)
            {
                foreach (var faultedActivity in fault)
                {
                    // An activity caused the fault.
                    workflowInstance.WorkflowStatus = faultedActivity.Resuming ? WorkflowStatus.Suspended : WorkflowStatus.Running;
                    workflowInstance.ScheduledActivities.Push(new ScheduledActivity(faultedActivity.FaultedActivityId, faultedActivity.ActivityInput));
                }
            }

            else
            {
                // The workflow failed before or after running an activity.
                // If no activities were scheduled, the workflow faulted before finishing executing the first activity.
                var hasScheduledActivities = workflowInstance.CurrentActivity != null;
                workflowInstance.WorkflowStatus = !hasScheduledActivities ? WorkflowStatus.Idle : WorkflowStatus.Running;
            }
           

            workflowInstance.Faults.Clear();
            workflowInstance.FaultedAt = null;
            await _workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
            return workflowInstance;
        }

        public async Task<RunWorkflowResult> ReviveAndRunAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
        {
            workflowInstance = await ReviveAsync(workflowInstance, cancellationToken);
            return await _resumesWorkflow.ResumeWorkflowAsync(workflowInstance, cancellationToken: cancellationToken);
        }

        public async Task<WorkflowInstance> ReviveAndQueueAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
        {
            workflowInstance = await ReviveAsync(workflowInstance, cancellationToken);
            var scheduledActivities = await GetActivityToScheduleAsync(workflowInstance, cancellationToken);
            foreach (var activity in scheduledActivities)
            {
                await _workflowInstanceDispatcher.DispatchAsync(new ExecuteWorkflowInstanceRequest(workflowInstance.Id, activity.ActivityId, new WorkflowInput(activity.Input)), cancellationToken);

            }
            return workflowInstance;
        }

        private async Task<List<ScheduledActivity>> GetActivityToScheduleAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
        {
            var currentActivity = workflowInstance.CurrentActivity;

            if (currentActivity != null)
                return new List<ScheduledActivity>() { currentActivity };
            if (workflowInstance.ScheduledActivities.Count > 0)
            {
                return workflowInstance.ScheduledActivities;
            }

            var workflowBlueprint = await _workflowRegistry.GetWorkflowAsync(workflowInstance.DefinitionId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);

            if (workflowBlueprint == null)
                throw new WorkflowException($"Could not find associated workflow definition {workflowInstance.DefinitionId} with version {workflowInstance.Version}");

            var startActivity = _startingActivitiesProvider.GetStartActivities(workflowBlueprint).FirstOrDefault();

            if (startActivity == null)
                throw new WorkflowException($"Cannot revive workflow {workflowInstance.Id} because it has no start activities");

            return new List<ScheduledActivity>() { new ScheduledActivity(startActivity.Id) };
        }
    }
}