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

        public StoreWorkflowProvider(IWorkflowDefinitionStore store)
        {
            this.store = store;
        }

        public async Task<IEnumerable<Workflow>> GetWorkflowsAsync(CancellationToken cancellationToken)
        {
            var workflowDefinitions = await store.ListAsync(VersionOptions.All, cancellationToken);
            return workflowDefinitions.Select(CreateWorkflow);
        }

        private Workflow CreateWorkflow(WorkflowDefinitionVersion definition)
        {
            var workflow = new Workflow
            {
                DefinitionId = definition.Id,
                Description = definition.Description, 
                Name = definition.Name,
                Version = definition.Version,
                IsLatest = definition.IsLatest,
                IsPublished = definition.IsPublished,
                IsDisabled = definition.IsDisabled,
                IsSingleton = definition.IsSingleton,
                PersistenceBehavior = definition.PersistenceBehavior,
                DeleteCompletedInstances = definition.DeleteCompletedInstances
            };

            return workflow;
        }
    }
}