using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Persistence.Entities;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Provides methods to add and remove bookmarks.
/// </summary>
public interface IBookmarkManager
{
    Task DeleteAsync(IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default);
    Task SaveAsync(WorkflowInstance workflowInstance, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default);
    Task<Bookmark?> FindByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bookmark>> FindManyByWorkflowInstanceIdAsync(string workflowInstanceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkflowBookmark>> FindManyByHashAsync(string bookmarkName, string hash, CancellationToken cancellationToken = default);
}