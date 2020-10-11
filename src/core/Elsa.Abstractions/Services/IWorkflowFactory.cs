using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowFactory
    {
        Task<WorkflowInstance> InstantiateAsync(
            WorkflowDefinition workflowDefinition, 
            string? correlationId = default, 
            CancellationToken cancellationToken = default);
    }
}