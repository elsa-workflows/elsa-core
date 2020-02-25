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
        private readonly IWorkflowDefinitionStore store;
        private readonly IActivityResolver activityResolver;

        public StoreWorkflowProvider(IWorkflowDefinitionStore store, IActivityResolver activityResolver)
        {
            this.store = store;
            this.activityResolver = activityResolver;
        }

        public async Task<IEnumerable<Workflow>> GetWorkflowsAsync(CancellationToken cancellationToken)
        {
            var workflowDefinitions = await store.ListAsync(VersionOptions.All, cancellationToken);
            return workflowDefinitions.Select(CreateWorkflow);
        }

        private Workflow CreateWorkflow(WorkflowDefinitionVersion definition)
        {
            var resolvedActivities = definition.Activities.Select(ResolveActivity).ToDictionary(x => x.Id);

            var workflow = new Workflow
            {
                DefinitionId = definition.DefinitionId,
                Description = definition.Description,
                Name = definition.Name,
                Version = definition.Version,
                IsLatest = definition.IsLatest,
                IsPublished = definition.IsPublished,
                IsDisabled = definition.IsDisabled,
                IsSingleton = definition.IsSingleton,
                PersistenceBehavior = definition.PersistenceBehavior,
                DeleteCompletedInstances = definition.DeleteCompletedInstances,
                Activities = resolvedActivities.Values,
                Connections = definition.Connections.Select(x => ResolveConnection(x, resolvedActivities)).ToList()
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