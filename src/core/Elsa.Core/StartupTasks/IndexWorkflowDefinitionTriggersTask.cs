using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence;
using Elsa.Services;

namespace Elsa.StartupTasks
{
    public class IndexWorkflowDefinitionTriggersTask : IStartupTask
    {
        private readonly IWorkflowTriggerStore _workflowTriggerStore;

        public IndexWorkflowDefinitionTriggersTask(IWorkflowTriggerStore workflowTriggerStore, IWorkflowRegistry workflowRegistry)
        {
            _workflowTriggerStore = workflowTriggerStore;
        }
        
        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}