using System.Threading;
using System.Threading.Tasks;

using Elsa.Models;

namespace Elsa.Indexing.Services
{
    public class WorkflowInstanceIndexer : IWorkflowInstanceIndexer
    {
        public Task DeleteAsync(WorkflowInstance instance, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task IndexAsync(WorkflowInstance instance, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
