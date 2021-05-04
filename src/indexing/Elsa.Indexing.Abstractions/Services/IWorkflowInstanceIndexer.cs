using System.Threading;
using System.Threading.Tasks;

using Elsa.Models;

namespace Elsa.Indexing.Services
{
    public interface IWorkflowInstanceIndexer
    {
        Task IndexAsync(WorkflowInstance instance, CancellationToken cancellationToken);
        Task DeleteAsync(WorkflowInstance instance, CancellationToken cancellationToken);
    }
}
