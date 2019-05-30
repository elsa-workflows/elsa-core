using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Persistence.InMemory
{
    public interface IInMemoryWorkflowProvider
    {
        Task SaveAsync(Workflow value, CancellationToken cancellationToken);
        Task<IEnumerable<Workflow>> ListAsync(CancellationToken cancellationToken);
    }
}