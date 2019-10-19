using System.Linq;
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
        private readonly IWorkflowDefinitionStore workflowDefinitionStore;
        private readonly IWorkflowRegistry workflowRegistry;

        public PopulateRegistryTask(
            IWorkflowDefinitionStore workflowDefinitionStore,
            IWorkflowRegistry workflowRegistry)
        {
            this.workflowDefinitionStore = workflowDefinitionStore;
            this.workflowRegistry = workflowRegistry;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var query =
                await workflowDefinitionStore.ListAsync(VersionOptions.All, cancellationToken);

            var workflowDefinitions = query.Where(x => !x.IsDisabled).ToList();
            workflowRegistry.RegisterWorkflows(workflowDefinitions);
        }
    }
}