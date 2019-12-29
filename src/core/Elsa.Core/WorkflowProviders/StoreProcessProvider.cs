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
    public class StoreProcessProvider : IProcessProvider
    {
        private readonly IWorkflowDefinitionStore store;
        private readonly IProcessFactory processFactory;

        public StoreProcessProvider(IWorkflowDefinitionStore store, IProcessFactory processFactory)
        {
            this.store = store;
            this.processFactory = processFactory;
        }

        public async Task<IEnumerable<Process>> GetProcessesAsync(CancellationToken cancellationToken)
        {
            var workflowDefinitions = await store.ListAsync(VersionOptions.All, cancellationToken);
            return workflowDefinitions.Select(CreateWorkflowBlueprint);
        }

        private Process CreateWorkflowBlueprint(ProcessDefinitionVersion definition) => processFactory.CreateProcess(definition);
    }
}