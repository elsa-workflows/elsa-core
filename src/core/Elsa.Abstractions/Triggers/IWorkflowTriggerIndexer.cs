using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Triggers
{
    public interface IWorkflowTriggerIndexer
    {
        Task IndexTriggersAsync(IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken);
        Task IndexTriggersAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken);
    }
}