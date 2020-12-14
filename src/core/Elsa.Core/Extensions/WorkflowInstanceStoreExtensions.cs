using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;

namespace Elsa.Extensions
{
    public static class WorkflowInstanceStoreExtensions
    {
        public static Task<WorkflowInstance?> FindByIdAsync(
           this IWorkflowInstanceStore store,
           string id,
           CancellationToken cancellationToken = default) =>
           store.FindAsync(new WorkflowInstanceIdSpecification(id), cancellationToken);
    }
}
