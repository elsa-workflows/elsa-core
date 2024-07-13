using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Provides access to bookmark queue items.
/// </summary>
public interface IBookmarkQueueItemStore
{
    /// <summary>
    /// Adds or updates the specified <see cref="BookmarkQueueItem"/> in the persistence store.
    /// </summary>
    /// <remarks>
    /// If the record does not already exist, it is added to the store; if it does exist, its existing entry is updated.
    /// </remarks>
    Task SaveAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default);

    /// Adds the specified <see cref="BookmarkQueueItem"/> to the persistence store.
    Task AddAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default);

    /// Returns the first bookmark queue item matching the specified filter.
    Task<BookmarkQueueItem?> FindAsync(BookmarkQueueItemFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a page of records, ordered by the specified order definition.
    /// </summary>
    Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a set of bookmark queue items matching the specified filter.
    /// </summary>
    /// <returns>The number of deleted records.</returns>
    Task<long> DeleteAsync(BookmarkQueueItemFilter filter, CancellationToken cancellationToken = default);
}