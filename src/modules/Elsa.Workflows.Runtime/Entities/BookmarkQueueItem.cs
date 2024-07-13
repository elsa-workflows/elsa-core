using Elsa.Common.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime.Entities;

/// Represents a bookmark queue item that is used to resume bookmarks that may not yet have been stored.
public class BookmarkQueueItem : Entity
{
    /// The workflow instance ID owning the bookmark to resume.
    public string WorkflowInstanceId { get; set; } = default!;
    
    /// The bookmark ID to resume.
    public string? BookmarkId { get; set; } = default!;
    
    /// A bookmark payload hash of the bookmark to resume.
    public string? BookmarkHash { get; set; }
    
    /// Any options to apply when resuming the bookmark.
    public ResumeBookmarkOptions? Options { get; set; }
    
    /// The timestamp that this entity was created.
    public DateTimeOffset CreatedAt { get; set; }

    public BookmarkFilter CreateBookmarkFilter()
    {
        return new BookmarkFilter
        {
            WorkflowInstanceId = WorkflowInstanceId,
            BookmarkId = BookmarkId,
            Hash = BookmarkHash,
        };
    }
}