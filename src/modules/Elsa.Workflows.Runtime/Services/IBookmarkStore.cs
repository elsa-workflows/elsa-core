using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Services;

public interface IBookmarkStore
{
    ValueTask SaveAsync(string activityTypeName, string hash, string workflowInstanceId, IEnumerable<string> bookmarkIds, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<StoredBookmark>> FindByWorkflowInstanceAsync(string workflowInstanceId, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<StoredBookmark>> FindByHashAsync(string hash, CancellationToken cancellationToken = default);
    ValueTask DeleteAsync(string hash, string workflowInstanceId, CancellationToken cancellationToken = default);
}