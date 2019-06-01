using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Persistence
{
    public interface IWorkflowStore
    {
        Task SaveAsync(Workflow workflow, CancellationToken cancellationToken);
        Task<Workflow> GetByIdAsync(string id, CancellationToken cancellationToken);
        Task<IEnumerable<Workflow>> ListAllAsync(string parentId, CancellationToken cancellationToken);
        Task<IEnumerable<Workflow>> ListAllAsync(CancellationToken cancellationToken);

    }
}