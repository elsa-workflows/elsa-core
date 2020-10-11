using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Data.Services
{
    /// <summary>
    /// Provides workflows from the workflow definition store.
    /// </summary>
    public class DatabaseWorkflowProvider : IWorkflowProvider
    {
        private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
        private readonly IActivityResolver _activityResolver;

        public DatabaseWorkflowProvider(
            IWorkflowDefinitionManager workflowDefinitionManager,
            IActivityResolver activityResolver)
        {
            _workflowDefinitionManager = workflowDefinitionManager;
            _activityResolver = activityResolver;
        }

        public async Task<IEnumerable<Workflow>> GetWorkflowsAsync(CancellationToken cancellationToken)
        {
            var workflowDefinitions = await _workflowDefinitionManager.ListAsync(cancellationToken);
            return workflowDefinitions.Select(CreateWorkflow);
        }

        private Workflow CreateWorkflow(WorkflowDefinition definition)
        {
            var resolvedActivities = definition.Activities.Select(ResolveActivity).ToDictionary(x => x.Id);

            var workflow = new Workflow(
                definition.WorkflowDefinitionVersionId,
                definition.Version,
                definition.IsSingleton,
                definition.IsEnabled,
                definition.Name,
                definition.Description,
                definition.IsLatest,
                definition.IsPublished,
                definition.PersistenceBehavior,
                definition.DeleteCompletedInstances,
                resolvedActivities.Values,
                definition.Connections.Select(x => ResolveConnection(x, resolvedActivities)).ToList(),
                new Dictionary<string, IDictionary<string, IActivityPropertyValueProvider>>()
            );

            return workflow;
        }

        private static Connection ResolveConnection(
            ConnectionDefinition connectionDefinition,
            IReadOnlyDictionary<string, IActivity> activityDictionary)
        {
            var source = activityDictionary[connectionDefinition.SourceActivityId!];
            var target = activityDictionary[connectionDefinition.TargetActivityId!];
            var outcome = connectionDefinition.Outcome;

            return new Connection(source, target, outcome!);
        }

        private IActivity ResolveActivity(ActivityDefinition activityDefinition) =>
            _activityResolver.ResolveActivity(activityDefinition);
    }
}