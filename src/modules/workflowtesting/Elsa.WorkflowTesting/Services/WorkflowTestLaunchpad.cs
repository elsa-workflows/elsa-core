using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.WorkflowTesting.Services
{
    public class WorkflowTestLaunchpad : IWorkflowTestLaunchpad
    {
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IWorkflowFactory _workflowFactory;
        private readonly IWorkflowRunner _workflowRunner;

        public WorkflowTestLaunchpad(
            IWorkflowRegistry workflowRegistry,
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowFactory workflowFactory,
            IWorkflowRunner workflowRunner)
        {
            _workflowRegistry = workflowRegistry;
            _workflowInstanceStore = workflowInstanceStore;
            _workflowFactory = workflowFactory;
            _workflowRunner = workflowRunner;
        }

        public async Task<RunWorkflowResult?> FindAndRestartTestWorkflowAsync(
            string workflowDefinitionId,
            string activityId,
            int version,
            string signalRConnectionId,
            string lastWorkflowInstanceId,
            string? tenantId = default,
            CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = await _workflowRegistry.FindAsync(workflowDefinitionId, VersionOptions.SpecificVersion(version), tenantId, cancellationToken);

            if (workflowBlueprint == null)
                return null;

            var lastWorkflowInstance = await _workflowInstanceStore.FindAsync(new EntityIdSpecification<WorkflowInstance>(lastWorkflowInstanceId), cancellationToken);

            if (lastWorkflowInstance == null)
                return null;

            var startActivity = workflowBlueprint.Activities.First(x => x.Id == activityId);

            var startableWorkflowDefinition = new StartableWorkflowDefinition(workflowBlueprint, startActivity.Id);

            var workflow = await InstantiateStartableWorkflow(startableWorkflowDefinition, cancellationToken);

            var previousActivityData = GetActivityDataFromLastWorkflowInstance(lastWorkflowInstance, workflowBlueprint, activityId);

            MergeActivityDataIntoInstance(workflow.WorkflowInstance, previousActivityData);

            SetMetadata(workflow.WorkflowInstance, signalRConnectionId);

            //if previousActivityOutput has any items, then the first one is from activity closest to the starting one  
            var previousActivityOutput = previousActivityData.Count == 0 ? null : previousActivityData.First().Value?.GetItem("Output");

            return await ExecuteStartableWorkflowAsync(workflow, new WorkflowInput(previousActivityOutput), cancellationToken);
        }

        private void SetMetadata(WorkflowInstance workflowInstance, string signalRConnectionId)
        {
            workflowInstance.SetMetadata("isTest", true);
            workflowInstance.SetMetadata("signalRConnectionId", signalRConnectionId);
        }

        private void MergeActivityDataIntoInstance(WorkflowInstance workflowInstance, IDictionary<string, IDictionary<string, object?>> activityData)
        {
            foreach (var (key, value) in activityData)
            {
                workflowInstance.ActivityData[key] = value;
            }
        }

        private IDictionary<string, IDictionary<string, object?>> GetActivityDataFromLastWorkflowInstance(WorkflowInstance lastWorkflowInstance, IWorkflowBlueprint workflowBlueprint, string startingActivityId)
        {
            IDictionary<string, IDictionary<string, object?>> CollectSourceActivityData(string targetActivityId, IDictionary<string, IDictionary<string, object?>> activityDataAccumulator)
            {
                var sourceActivityId = workflowBlueprint.Connections.FirstOrDefault(x => x.Target.Activity.Id == targetActivityId)?.Source.Activity.Id;

                if (sourceActivityId == null)
                    return activityDataAccumulator;

                activityDataAccumulator.Add(sourceActivityId, lastWorkflowInstance.ActivityData.GetItem(sourceActivityId));

                return CollectSourceActivityData(sourceActivityId, activityDataAccumulator);
            }

            return CollectSourceActivityData(startingActivityId, new Dictionary<string, IDictionary<string, object?>>());
        }

        private async Task<StartableWorkflow> InstantiateStartableWorkflow(StartableWorkflowDefinition startableWorkflowDefinition, CancellationToken cancellationToken)
        {
            var workflowInstance = await _workflowFactory.InstantiateAsync(
                startableWorkflowDefinition.WorkflowBlueprint,
                startableWorkflowDefinition.CorrelationId,
                startableWorkflowDefinition.ContextId,
                cancellationToken: cancellationToken);

            await _workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
            return new StartableWorkflow(startableWorkflowDefinition.WorkflowBlueprint, workflowInstance, startableWorkflowDefinition.ActivityId);
        }

        private async Task<RunWorkflowResult> ExecuteStartableWorkflowAsync(StartableWorkflow startableWorkflow, WorkflowInput? input, CancellationToken cancellationToken = default) =>
            await _workflowRunner.RunWorkflowAsync(startableWorkflow.WorkflowBlueprint, startableWorkflow.WorkflowInstance, startableWorkflow.ActivityId, input, cancellationToken);
    }
}