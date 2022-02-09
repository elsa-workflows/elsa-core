using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions.MultiTenancy;
using Elsa.Models;

namespace Elsa.Services
{
    public interface IBookmarkIndexer
    {
        Task IndexBookmarksAsync(WorkflowInstance workflowInstance, Tenant tenant, CancellationToken cancellationToken = default);
        Task DeleteBookmarksAsync(string workflowInstanceId, Tenant tenant, CancellationToken cancellationToken = default);
    }
}