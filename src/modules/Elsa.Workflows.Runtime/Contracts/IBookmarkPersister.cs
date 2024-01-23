using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Persists bookmarks and raises events.
/// </summary>
public interface IBookmarksPersister
{
    /// <summary>
    /// Persists bookmarks and raises events.
    /// </summary>
    Task PersistBookmarksAsync(UpdateBookmarksRequest updateBookmarksRequest);
}