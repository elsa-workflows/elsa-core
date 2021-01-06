using System.Threading;
using System.Threading.Tasks;

using Elsa.Models;

namespace Elsa.Indexing.Services
{
    public interface IWorkflowDefinitionIndexer
    {
        Task IndexAsync(WorkflowDefinition instance, CancellationToken cancellationToken);
        Task DeleteAsync(WorkflowDefinition instance, CancellationToken cancellationToken);
    }
}
