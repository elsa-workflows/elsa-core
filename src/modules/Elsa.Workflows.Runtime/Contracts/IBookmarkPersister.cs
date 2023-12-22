using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Persists bookmarks and raises events.
/// </summary>
public interface IBookmarksPersister
{
    /// <summary>
    /// Persists bookmarks and raises events.
    /// </summary>
    Task PersistBookmarksAsync(WorkflowExecutionContext context, Diff<Bookmark> diff);
}