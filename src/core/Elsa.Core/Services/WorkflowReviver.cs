using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Models;

namespace Elsa.Services
{
    public class WorkflowReviver : IWorkflowReviver
    {
        private readonly IWorkflowRunner _workflowRunner;
        private readonly IWorkflowQueue _workflowQueue;
        private readonly IWorkflowRegistry _workflowRegistry;

        public WorkflowReviver(IWorkflowRunner workflowRunner, IWorkflowQueue workflowQueue, IWorkflowRegistry workflowRegistry)
        {
            _workflowRunner = workflowRunner;
            _workflowQueue = workflowQueue;
            _workflowRegistry = workflowRegistry;
        }
        
        public WorkflowInstance Revive(WorkflowInstance workflowInstance)
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
            }
            
            return workflowInstance;
        }

        public async Task<WorkflowInstance> ReviveAndRunAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
        {
            workflowInstance = Revive(workflowInstance);
            return await _workflowRunner.RunWorkflowAsync(workflowInstance, null, null, cancellationToken);
        }
        
        public async Task<WorkflowInstance> ReviveAndQueueAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
        {
            workflowInstance = Revive(workflowInstance);
            var currentActivity = await GetActivityToScheduleAsync(workflowInstance, cancellationToken); 
            await _workflowQueue.EnqueueWorkflowInstance(workflowInstance.Id, currentActivity.ActivityId, currentActivity.Input, cancellationToken);
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
            
            var startActivity = workflowBlueprint.GetStartActivities().FirstOrDefault();

            if (startActivity == null)
                throw new WorkflowException($"Cannot revive workflow {workflowInstance.Id} because it has no start activities");

            return new ScheduledActivity(startActivity.Id);
        }
    }
}