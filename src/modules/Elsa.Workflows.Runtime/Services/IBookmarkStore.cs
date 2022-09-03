using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Services;

public interface IBookmarkStore
{
    ValueTask SaveAsync(string hash, string workflowInstanceId, IEnumerable<string> bookmarkIds, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<StoredBookmark>> LoadAsync(string workflowInstanceId, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<StoredBookmark>> LoadAsync(string activityTypeName, string hash, CancellationToken cancellationToken = default);
    ValueTask DeleteAsync(string hash, string workflowInstanceId, CancellationToken cancellationToken = default);
}