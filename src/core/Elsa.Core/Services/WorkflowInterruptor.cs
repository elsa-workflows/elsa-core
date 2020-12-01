using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public class WorkflowInterruptor : IWorkflowInterruptor
    {
        private readonly IWorkflowRunner _workflowRunner;
        private readonly IWorkflowRegistry _workflowRegistry;

        public WorkflowInterruptor(IWorkflowRunner workflowRunner, IWorkflowRegistry workflowRegistry)
        {
            _workflowRunner = workflowRunner;
            _workflowRegistry = workflowRegistry;
        }

        public async Task<WorkflowInstance> InterruptActivityAsync(WorkflowInstance workflowInstance, string activityId, object? input, CancellationToken cancellationToken)
        {
            var workflowBlueprint = await GetWorkflowBlueprintAsync(workflowInstance, cancellationToken);
            return await InterruptActivityAsync(workflowBlueprint!, workflowInstance, activityId, input, cancellationToken);
        }

        public async Task<WorkflowInstance> InterruptActivityAsync(IWorkflowBlueprint workflowBlueprint, WorkflowInstance workflowInstance, string activityId, object? input, CancellationToken cancellationToken)
        {
            if (workflowInstance.Status != WorkflowStatus.Suspended)
                throw new WorkflowException("Cannot interrupt workflows that are not in the Suspended state.");

            var blockingActivity = workflowInstance.BlockingActivities.SingleOrDefault(x => x.ActivityId == activityId);

            if (blockingActivity == null)
                throw new WorkflowException($"No blocking activity with ID {activityId} found.");

            return await _workflowRunner.RunWorkflowAsync(workflowBlueprint, workflowInstance, activityId, input, cancellationToken);
        }

        public async Task<WorkflowInstance> InterruptActivityTypeAsync(IWorkflowBlueprint workflowBlueprint, WorkflowInstance workflowInstance, string activityType, object? input, CancellationToken cancellationToken)
        {
            var blockingActivities = workflowInstance.BlockingActivities.Where(x => x.ActivityType == activityType).ToList();

            foreach (var blockingActivity in blockingActivities)
                workflowInstance = await InterruptActivityAsync(workflowBlueprint, workflowInstance, blockingActivity.ActivityId, input, cancellationToken);

            return workflowInstance;
        }

        public async Task<WorkflowInstance> InterruptActivityTypeAsync(WorkflowInstance workflowInstance, string activityType, object? input, CancellationToken cancellationToken)
        {
            var workflowBlueprint = await GetWorkflowBlueprintAsync(workflowInstance, cancellationToken);
            return await InterruptActivityTypeAsync(workflowBlueprint!, workflowInstance, activityType, input, cancellationToken);
        }

        private async Task<IWorkflowBlueprint?> GetWorkflowBlueprintAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken) => 
            await _workflowRegistry.GetWorkflowAsync(workflowInstance.WorkflowDefinitionId, workflowInstance.TenantId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);
    }
}