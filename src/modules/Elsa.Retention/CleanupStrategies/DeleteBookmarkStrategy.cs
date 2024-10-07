using Elsa.Retention.Contracts;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.Logging;

namespace Elsa.Retention.CleanupStrategies;

/// <summary>
///     Deletes a collection of bookmarks
/// </summary>
public class DeleteBookmarkStrategy : IDeletionCleanupStrategy<StoredBookmark>
{
    private readonly ILogger<DeleteBookmarkStrategy> _logger;
    private readonly IBookmarkStore _store;

    public DeleteBookmarkStrategy(IBookmarkStore store, ILogger<DeleteBookmarkStrategy> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task Cleanup(ICollection<StoredBookmark> collection)
    {
        BookmarkFilter bookmarkFilter = new()
        {
            BookmarkIds = collection.Select(x => x.Id).ToList()
        };

        long deletedRecords = await _store.DeleteAsync(bookmarkFilter);

        if (deletedRecords != collection.Count)
        {
            _logger.LogWarning("Expected to delete {Expected} bookmarks, actually deleted {Actual} bookmarks", collection.Count, deletedRecords);
        }
    }
}