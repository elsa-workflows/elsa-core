using System.Threading;
using System.Threading.Tasks;

using Elsa.Models;

namespace Elsa.Indexing.Services
{
    public class WorkflowDefinitionIndexer : IWorkflowDefinitionIndexer
    {
        public Task DeleteAsync(WorkflowDefinition instance, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task IndexAsync(WorkflowDefinition instance, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
