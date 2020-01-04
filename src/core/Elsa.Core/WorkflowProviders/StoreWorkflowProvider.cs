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
        private readonly IWorkflowFactory workflowFactory;

        public StoreWorkflowProvider(IWorkflowDefinitionStore store, IWorkflowFactory workflowFactory)
        {
            this.store = store;
            this.workflowFactory = workflowFactory;
        }

        public async Task<IEnumerable<Workflow>> GetProcessesAsync(CancellationToken cancellationToken)
        {
            var workflowDefinitions = await store.ListAsync(VersionOptions.All, cancellationToken);
            return workflowDefinitions.Select(CreateWorkflowBlueprint);
        }

        private Workflow CreateWorkflowBlueprint(ProcessDefinitionVersion definition) => workflowFactory.CreateProcess(definition);
    }
}