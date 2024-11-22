using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Contains extension methods for <see cref="Bookmark"/>.
/// </summary>
public static class BookmarkExtensions
{
    /// <summary>
    /// Converts a <see cref="Bookmark"/> to a <see cref="BookmarkInfo"/>.
    /// </summary>
    public static BookmarkInfo ToBookmarkInfo(this Bookmark source) => new()
    {
        Hash = source.Hash,
        Id = source.Id,
        Name = source.Name
    };
    
    /// <summary>
    /// Converts a collection of <see cref="Bookmark"/> to a collection of <see cref="BookmarkInfo"/>.
    /// </summary>
    public static IEnumerable<BookmarkInfo> ToBookmarkInfos(this IEnumerable<Bookmark> source)
    {
        return source.Select(ToBookmarkInfo);
    }
    
    public static IEnumerable<StoredBookmark> MapBookmarks(this WorkflowExecutionContext workflowExecutionContext, IEnumerable<Bookmark> bookmarks)
    {
        return bookmarks.Select(workflowExecutionContext.MapBookmark);
    }
    
    public static StoredBookmark MapBookmark(this WorkflowExecutionContext workflowExecutionContext, Bookmark bookmark)
    {
        return new StoredBookmark
        {
            Id = bookmark.Id,
            ActivityTypeName = bookmark.Name,
            Hash = bookmark.Hash,
            WorkflowInstanceId = workflowExecutionContext.Id,
            CreatedAt = bookmark.CreatedAt,
            ActivityInstanceId = bookmark.ActivityInstanceId,
            CorrelationId = workflowExecutionContext.CorrelationId,
            Payload = bookmark.Payload,
            Metadata = bookmark.Metadata
        };
    }
}