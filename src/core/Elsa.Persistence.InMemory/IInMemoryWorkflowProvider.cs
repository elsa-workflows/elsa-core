using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Serialization.Models;

namespace Elsa.Persistence.InMemory
{
    public interface IInMemoryWorkflowProvider
    {
        Task SaveAsync(WorkflowInstance value, CancellationToken cancellationToken);
        Task<IEnumerable<WorkflowInstance>> ListAsync(CancellationToken cancellationToken);
    }
}