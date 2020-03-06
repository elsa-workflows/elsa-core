using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
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
        private readonly IWorkflowDefinitionVersionStore store;
        private readonly IActivityResolver activityResolver;

        public StoreWorkflowProvider(IWorkflowDefinitionVersionStore store, IActivityResolver activityResolver)
        {
            this.store = store;
            this.activityResolver = activityResolver;
        }

        public async Task<IEnumerable<Workflow>> GetWorkflowsAsync(CancellationToken cancellationToken)
        {
            var workflowDefinitionVersions = await store.ListAsync(VersionOptions.All, cancellationToken);
            return workflowDefinitionVersions.Select(CreateWorkflow);
        }

        private Workflow CreateWorkflow(WorkflowDefinitionVersion definitionVersion)
        {
            var resolvedActivities = definitionVersion.Activities.Select(ResolveActivity).ToDictionary(x => x.Id);

            var workflow = new Workflow
            {
                DefinitionId = definitionVersion.DefinitionId,
                Description = definitionVersion.Description,
                Name = definitionVersion.Name,
                Version = definitionVersion.Version,
                IsLatest = definitionVersion.IsLatest,
                IsPublished = definitionVersion.IsPublished,
                IsDisabled = definitionVersion.IsDisabled,
                IsSingleton = definitionVersion.IsSingleton,
                PersistenceBehavior = definitionVersion.PersistenceBehavior,
                DeleteCompletedInstances = definitionVersion.DeleteCompletedInstances,
                Activities = resolvedActivities.Values,
                Connections = definitionVersion.Connections.Select(x => ResolveConnection(x, resolvedActivities)).ToList()
            };

            return workflow;
        }

        private static Connection ResolveConnection(ConnectionDefinition connectionDefinition, IReadOnlyDictionary<string?, IActivity> activityDictionary)
        {
            var source = activityDictionary[connectionDefinition.SourceActivityId];
            var target = activityDictionary[connectionDefinition.TargetActivityId];
            var outcome = connectionDefinition.Outcome;

            return new Connection(source, target, outcome);
        }

        private IActivity ResolveActivity(ActivityDefinition activityDefinition) => activityResolver.ResolveActivity(activityDefinition);
    }
}