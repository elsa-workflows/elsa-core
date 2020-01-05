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
        private readonly IWorkflowDefinitionStore store;
        private readonly IActivityResolver activityResolver;

        public StoreWorkflowProvider(IWorkflowDefinitionStore store, IActivityResolver activityResolver)
        {
            this.store = store;
            this.activityResolver = activityResolver;
        }

        public async Task<IEnumerable<Workflow>> GetProcessesAsync(CancellationToken cancellationToken)
        {
            var workflowDefinitions = await store.ListAsync(VersionOptions.All, cancellationToken);
            return workflowDefinitions.Select(CreateWorkflow);
        }

        private Workflow CreateWorkflow(WorkflowDefinitionVersion definition)
        {
            var workflow = new Workflow
            {
                Description = definition.Description, 
                Name = definition.Name, 
                Start = ResolveActivity(definition.Start),
                Version = definition.Version,
                Id = definition.Id,
                IsLatest = definition.IsLatest,
                IsPublished = definition.IsPublished,
                IsDisabled = definition.IsDisabled,
                IsSingleton = definition.IsSingleton,
                PersistenceBehavior = definition.PersistenceBehavior,
                DeleteCompletedInstances = definition.DeleteCompletedInstances
            };

            return workflow;
        }

        private IActivity ResolveActivity(ActivityDefinition activityDefinition)
        {
            var activity = activityResolver.ResolveActivity(activityDefinition.Type);
            activity.Description = activityDefinition.Description;
            activity.Id = activityDefinition.Id;
            activity.Name = activityDefinition.Name;
            activity.DisplayName = activityDefinition.DisplayName;
            activity.State = activityDefinition.State;
            return activity;
        }
    }
}