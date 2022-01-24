using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services
{
    public interface IBookmarkIndexer
    {
        Task IndexBookmarksAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);
        Task DeleteBookmarksAsync(string workflowInstanceId, CancellationToken cancellationToken = default);
    }
}