using Elsa.Retention.Contracts;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.Logging;

namespace Elsa.Retention.CleanupStrategies;

/// <summary>
///     Deletes a collection of bookmarks
/// </summary>
public class DeleteBookmarkStrategy(IBookmarkStore store, ILogger<DeleteBookmarkStrategy> logger) : IDeletionCleanupStrategy<StoredBookmark>
{
    public async Task Cleanup(ICollection<StoredBookmark> collection)
    {
        var bookmarkFilter = new BookmarkFilter
        {
            BookmarkIds = collection.Select(x => x.Id).ToList()
        };

        var deletedRecords = await store.DeleteAsync(bookmarkFilter);

        if (deletedRecords != collection.Count)
        {
            logger.LogWarning("Expected to delete {Expected} bookmarks, actually deleted {Actual} bookmarks", collection.Count, deletedRecords);
        }
    }
}