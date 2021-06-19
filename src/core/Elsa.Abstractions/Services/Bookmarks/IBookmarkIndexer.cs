using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services.Bookmarks
{
    public interface IBookmarkIndexer
    {
        Task IndexBookmarksAsync(IEnumerable<WorkflowInstance> workflowInstances, CancellationToken cancellationToken = default);
        Task IndexBookmarksAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);
        Task DeleteBookmarksAsync(IEnumerable<string> workflowInstanceIds, CancellationToken cancellationToken = default);
        Task DeleteBookmarksAsync(string workflowInstanceId, CancellationToken cancellationToken = default);
    }
}