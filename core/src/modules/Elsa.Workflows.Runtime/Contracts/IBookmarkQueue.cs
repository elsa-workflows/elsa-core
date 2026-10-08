namespace Elsa.Workflows.Runtime;

/// <summary>
/// Stores bookmark parameters for matching bookmarks to resume when said bookmarks appear in the system.
/// If matching bookmarks are already present, they are resumed immediately and the parameters are not stored.
/// If matching parameters are detected when bookmarks enter the system, the parameters are removed.
/// </summary>
public interface IBookmarkQueue
{
    Task EnqueueAsync(NewBookmarkQueueItem item, CancellationToken cancellationToken = default);
}