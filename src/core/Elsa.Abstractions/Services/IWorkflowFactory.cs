using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowFactory
    {
        Task<WorkflowInstance> InstantiateAsync(
            IWorkflowBlueprint workflowBlueprint, 
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default);
    }
}