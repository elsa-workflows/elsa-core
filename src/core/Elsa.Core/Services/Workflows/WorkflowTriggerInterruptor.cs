using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Services.Bookmarks;
using Elsa.Services.Models;

namespace Elsa.Services.Workflows
{
    public class WorkflowTriggerInterruptor : IWorkflowTriggerInterruptor
    {
        private readonly IWorkflowRunner _workflowRunner;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IBookmarkFinder _bookmarkFinder;

        public WorkflowTriggerInterruptor(IWorkflowRunner workflowRunner, IWorkflowRegistry workflowRegistry, IWorkflowInstanceStore workflowInstanceStore, IBookmarkFinder bookmarkFinder)
        {
            _workflowRunner = workflowRunner;
            _workflowRegistry = workflowRegistry;
            _workflowInstanceStore = workflowInstanceStore;
            _bookmarkFinder = bookmarkFinder;
        }

        public async Task<RunWorkflowResult> InterruptActivityAsync(WorkflowInstance workflowInstance, string activityId, WorkflowInput? input, CancellationToken cancellationToken)
        {
            var workflowBlueprint = await GetWorkflowBlueprintAsync(workflowInstance, cancellationToken);
            return await InterruptActivityAsync(workflowBlueprint!, workflowInstance, activityId, input, cancellationToken);
        }

        public async Task<RunWorkflowResult> InterruptActivityAsync(IWorkflowBlueprint workflowBlueprint, WorkflowInstance workflowInstance, string activityId, WorkflowInput? input, CancellationToken cancellationToken)
        {
            if (workflowInstance.WorkflowStatus != WorkflowStatus.Suspended)
                throw new WorkflowException("Cannot interrupt workflows that are not in the Suspended state.");

            var blockingActivity = workflowInstance.BlockingActivities.SingleOrDefault(x => x.ActivityId == activityId);

            if (blockingActivity == null)
                throw new WorkflowException($"No blocking activity with ID {activityId} found.");

            return await _workflowRunner.RunWorkflowAsync(workflowBlueprint, workflowInstance, activityId, input, cancellationToken);
        }

        public async Task<IEnumerable<RunWorkflowResult>> InterruptActivityTypeAsync(IWorkflowBlueprint workflowBlueprint, WorkflowInstance workflowInstance, string activityType, WorkflowInput? input, CancellationToken cancellationToken)
        {
            var blockingActivities = workflowInstance.BlockingActivities.Where(x => x.ActivityType == activityType).ToList();
            var results = new List<RunWorkflowResult>();

            foreach (var blockingActivity in blockingActivities)
            {
                var result  = await InterruptActivityAsync(workflowBlueprint, workflowInstance, blockingActivity.ActivityId, input, cancellationToken);
                results.Add(result);
            }

            return results;
        }

        public async Task<IEnumerable<RunWorkflowResult>> InterruptActivityTypeAsync(WorkflowInstance workflowInstance, string activityType, WorkflowInput? input, CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = await GetWorkflowBlueprintAsync(workflowInstance, cancellationToken);
            return await InterruptActivityTypeAsync(workflowBlueprint!, workflowInstance, activityType, input, cancellationToken);
        }

        public async Task<IEnumerable<RunWorkflowResult>> InterruptActivityTypeAsync(string activityType, WorkflowInput? input, CancellationToken cancellationToken) =>
            await InterruptActivityTypeInternalAsync(activityType, input, cancellationToken).ToListAsync(cancellationToken);

        private async Task<IWorkflowBlueprint?> GetWorkflowBlueprintAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken) =>
            await _workflowRegistry.GetAsync(workflowInstance.DefinitionId, workflowInstance.TenantId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);

        private async IAsyncEnumerable<RunWorkflowResult> InterruptActivityTypeInternalAsync(string activityType, WorkflowInput? input, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var bookmarks = await _bookmarkFinder.FindBookmarksAsync(activityType, Enumerable.Empty<IBookmark>(), null, null, cancellationToken);
            var workflowInstanceIds = bookmarks.Select(x => x.WorkflowInstanceId).Distinct().ToList();
            var workflowInstances = await _workflowInstanceStore.FindManyAsync(new ManyWorkflowInstanceIdsSpecification(workflowInstanceIds), cancellationToken: cancellationToken);

            foreach (var workflowInstance in workflowInstances)
            {
                var results = await InterruptActivityTypeAsync(workflowInstance, activityType, input, cancellationToken);

                foreach (var result in results)
                    yield return result;
            }
        }
    }
}