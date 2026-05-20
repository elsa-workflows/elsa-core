using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Manages bookmark queue dead-letter items.
/// </summary>
public interface IBookmarkQueueDeadLetterManager
{
    /// <summary>
    /// Moves the specified queue item metadata into the dead-letter store.
    /// </summary>
    Task<BookmarkQueueDeadLetterItem> DeadLetterAsync(BookmarkQueueItem item, string reason, Exception? exception = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replays an eligible dead-letter item back into the bookmark queue.
    /// </summary>
    Task<ReplayBookmarkQueueDeadLetterResult> ReplayAsync(string id, CancellationToken cancellationToken = default);
}
