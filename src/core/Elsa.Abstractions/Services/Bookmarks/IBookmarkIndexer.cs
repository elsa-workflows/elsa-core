using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions.Multitenancy;
using Elsa.Models;

namespace Elsa.Services
{
    public interface IBookmarkIndexer
    {
        Task IndexBookmarksAsync(WorkflowInstance workflowInstance, ITenant tenant, CancellationToken cancellationToken = default);
        Task DeleteBookmarksAsync(string workflowInstanceId, ITenant tenant, CancellationToken cancellationToken = default);
    }
}