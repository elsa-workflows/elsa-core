using Elsa.Common.Models;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Provides access to bookmark queue dead-letter items.
/// </summary>
public interface IBookmarkQueueDeadLetterStore
{
    /// <summary>
    /// Adds or updates the specified <see cref="BookmarkQueueDeadLetterItem"/> in the persistence store.
    /// </summary>
    Task SaveAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds the specified <see cref="BookmarkQueueDeadLetterItem"/> to the persistence store.
    /// </summary>
    Task AddAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically marks a replayable dead-letter item as replayed.
    /// </summary>
    Task<BookmarkQueueDeadLetterItem?> TryMarkReplayedAsync(string id, string queueItemId, DateTimeOffset replayedAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the first bookmark queue dead-letter item matching the specified filter.
    /// </summary>
    Task<BookmarkQueueDeadLetterItem?> FindAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a set of bookmark queue dead-letter items matching the specified filter.
    /// </summary>
    Task<IEnumerable<BookmarkQueueDeadLetterItem>> FindManyAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a page of bookmark queue dead-letter items.
    /// </summary>
    Task<Page<BookmarkQueueDeadLetterItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueDeadLetterItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a page of bookmark queue dead-letter items.
    /// </summary>
    Task<Page<BookmarkQueueDeadLetterItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueDeadLetterFilter filter, BookmarkQueueDeadLetterItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a set of bookmark queue dead-letter items matching the specified filter.
    /// </summary>
    Task<long> DeleteAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default);
}
