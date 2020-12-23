using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Services.Models;
using Open.Linq.AsyncExtensions;

namespace Elsa.Services
{
    public class WorkflowTriggerInterruptor : IWorkflowTriggerInterruptor
    {
        private readonly IWorkflowRunner _workflowRunner;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;

        public WorkflowTriggerInterruptor(IWorkflowRunner workflowRunner, IWorkflowRegistry workflowRegistry, IWorkflowInstanceStore workflowInstanceStore)
        {
            _workflowRunner = workflowRunner;
            _workflowRegistry = workflowRegistry;
            _workflowInstanceStore = workflowInstanceStore;
        }

        public async Task<WorkflowInstance> InterruptActivityAsync(WorkflowInstance workflowInstance, string activityId, object? input, CancellationToken cancellationToken)
        {
            var workflowBlueprint = await GetWorkflowBlueprintAsync(workflowInstance, cancellationToken);
            return await InterruptActivityAsync(workflowBlueprint!, workflowInstance, activityId, input, cancellationToken);
        }

        public async Task<WorkflowInstance> InterruptActivityAsync(IWorkflowBlueprint workflowBlueprint, WorkflowInstance workflowInstance, string activityId, object? input, CancellationToken cancellationToken)
        {
            if (workflowInstance.WorkflowStatus != WorkflowStatus.Suspended)
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

        public async Task<IEnumerable<WorkflowInstance>> InterruptActivityTypeAsync(string activityType, object? input, CancellationToken cancellationToken) =>
            await InterruptActivityTypeInternalAsync(activityType, input, cancellationToken).ToListAsync(cancellationToken);

        private async Task<IWorkflowBlueprint?> GetWorkflowBlueprintAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken) =>
            await _workflowRegistry.GetWorkflowAsync(workflowInstance.DefinitionId, workflowInstance.TenantId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);

        private async IAsyncEnumerable<WorkflowInstance> InterruptActivityTypeInternalAsync(string activityType, object? input, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var suspendedWorkflows = await _workflowInstanceStore.FindManyAsync(new BlockingActivityTypeSpecification(activityType), cancellationToken: cancellationToken).ToList();
            var workflowInstanceIds = suspendedWorkflows.Select(x => x.Id).Distinct().ToList();
            var workflowInstances = await _workflowInstanceStore.FindManyAsync(new ManyWorkflowInstanceIdsSpecification(workflowInstanceIds), cancellationToken: cancellationToken).ToDictionary(x => x.Id);

            foreach (var suspendedWorkflow in suspendedWorkflows)
            {
                var workflowInstance = workflowInstances[suspendedWorkflow.Id];
                yield return await InterruptActivityTypeAsync(workflowInstance, activityType, input, cancellationToken);
            }
        }
    }
}