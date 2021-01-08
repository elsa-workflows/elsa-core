using System.Threading;
using System.Threading.Tasks;

using Elsa.Models;

namespace Elsa.Indexing.Services
{
    public interface IWorkflowDefinitionIndexer
    {
        Task IndexAsync(WorkflowDefinition definition, CancellationToken cancellationToken);
        Task DeleteAsync(WorkflowDefinition definition, CancellationToken cancellationToken);
    }
}
