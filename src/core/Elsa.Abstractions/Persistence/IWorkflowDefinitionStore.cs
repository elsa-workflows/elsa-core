using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Persistence
{
    public interface IWorkflowDefinitionStore
    {
        Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
        Task<WorkflowDefinition> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    }
}