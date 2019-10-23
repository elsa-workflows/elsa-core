using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;

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

        public async Task<IEnumerable<WorkflowDefinitionVersion>> GetWorkflowDefinitionsAsync(
            CancellationToken cancellationToken) => await store.ListAsync(VersionOptions.All, cancellationToken);
    }
}