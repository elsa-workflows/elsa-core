using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Runtime;
using Elsa.Services;

namespace Elsa.StartupTasks
{
    public class PopulateRegistryTask : IStartupTask
    {
        private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
        private readonly IWorkflowRegistry _workflowRegistry;

        public PopulateRegistryTask(IWorkflowDefinitionStore workflowDefinitionStore, IWorkflowRegistry workflowRegistry)
        {
            _workflowDefinitionStore = workflowDefinitionStore;
            _workflowRegistry = workflowRegistry;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var workflowDefinitions =
                await _workflowDefinitionStore.ListAsync(VersionOptions.Published, cancellationToken);
            
            _workflowRegistry.RegisterWorkflows(workflowDefinitions);
        }
    }
}