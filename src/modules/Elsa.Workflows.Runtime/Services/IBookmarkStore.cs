using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Services;

public interface IBookmarkStore
{
    ValueTask SaveAsync(string activityTypeName, string hash, string workflowInstanceId, IEnumerable<string> bookmarkIds, string? correlationId = default, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<StoredBookmark>> FindByWorkflowInstanceAsync(string workflowInstanceId, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<StoredBookmark>> FindByHashAsync(string hash, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<StoredBookmark>> FindByWorkflowInstanceAndHashAsync(string workflowInstanceId, string hash, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<StoredBookmark>> FindByCorrelationAndHashAsync(string correlationId, string hash, CancellationToken cancellationToken = default);
    ValueTask DeleteAsync(string hash, string workflowInstanceId, CancellationToken cancellationToken = default);
}