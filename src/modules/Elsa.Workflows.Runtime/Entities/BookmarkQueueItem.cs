using Elsa.Common.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime.Entities;

/// Represents a bookmark queue item that is used to resume bookmarks that may not yet have been stored.
public class BookmarkQueueItem : Entity
{
    /// The workflow instance ID owning the bookmark to resume.
    public string? WorkflowInstanceId { get; set; }

    /// The correlation ID associated with the workflow instance owning the bookmark to resume.
    public string? CorrelationId { get; set; }

    /// The bookmark ID to resume.
    public string? BookmarkId { get; set; }

    /// A bookmark payload hash of the bookmark to resume.
    public string? StimulusHash { get; set; }

    /// The ID of the activity instance associated with the bookmark.
    public string? ActivityInstanceId { get; set; }

    // The type name of the activity associated with the bookmark.
    public string? ActivityTypeName { get; set; }

    /// Any options to apply when resuming the bookmark.
    public ResumeBookmarkOptions? Options { get; set; }

    /// The timestamp that this entity was created.
    public DateTimeOffset CreatedAt { get; set; }

    /// Creates a <see cref="BookmarkFilter"/> from this bookmark queue item.
    public BookmarkFilter CreateBookmarkFilter()
    {
        return new BookmarkFilter
        {
            WorkflowInstanceId = WorkflowInstanceId,
            CorrelationId = CorrelationId,
            BookmarkId = BookmarkId,
            Hash = StimulusHash,
            ActivityInstanceId = ActivityInstanceId
        };
    }
}