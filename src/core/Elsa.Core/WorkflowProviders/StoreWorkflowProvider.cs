using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.WorkflowProviders
{
    /// <summary>
    /// Provides workflow definitions from the workflow definition store.
    /// </summary>
    public class StoreWorkflowProvider : IWorkflowProvider
    {
        private readonly IWorkflowDefinitionStore _store;
        private readonly IActivityResolver _activityResolver;

        public StoreWorkflowProvider(IWorkflowDefinitionStore store, IActivityResolver activityResolver)
        {
            this._store = store;
            this._activityResolver = activityResolver;
        }

        public async Task<IEnumerable<Workflow>> GetWorkflowsAsync(CancellationToken cancellationToken)
        {
            var workflowDefinitions = await _store.ListAsync(VersionOptions.All, cancellationToken);
            return workflowDefinitions.Select(CreateWorkflow);
        }

        private Workflow CreateWorkflow(WorkflowDefinitionVersion definition)
        {
            var resolvedActivities = definition.Activities.Select(ResolveActivity).ToDictionary(x => x.Id);

            var workflow = new Workflow
            (
                definition.DefinitionId,
                definition.Version,
                definition.IsSingleton,
                definition.IsDisabled,
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

        private static Connection ResolveConnection(ConnectionDefinition connectionDefinition, IReadOnlyDictionary<string?, IActivity> activityDictionary)
        {
            var source = activityDictionary[connectionDefinition.SourceActivityId];
            var target = activityDictionary[connectionDefinition.TargetActivityId];
            var outcome = connectionDefinition.Outcome;

            return new Connection(source, target, outcome!);
        }

        private IActivity ResolveActivity(ActivityDefinitionRecord activityDefinitionRecord) => _activityResolver.ResolveActivity(activityDefinitionRecord);
    }
}