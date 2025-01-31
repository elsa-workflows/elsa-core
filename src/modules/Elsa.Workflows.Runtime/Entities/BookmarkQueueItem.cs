using Elsa.Common.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime.Entities;

/// <summary>
/// Represents a bookmark queue item that is used to resume bookmarks that may not yet have been stored.
/// </summary>
public class BookmarkQueueItem : Entity
{
    /// <summary>
    /// The workflow instance ID owning the bookmark to resume.
    /// </summary>
    public string? WorkflowInstanceId { get; set; }

    /// <summary>
    /// The correlation ID associated with the workflow instance owning the bookmark to resume.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The bookmark ID to resume.
    /// </summary>
    public string? BookmarkId { get; set; }

    /// <summary>
    /// A bookmark payload hash of the bookmark to resume.
    /// </summary>
    public string? StimulusHash { get; set; }

    /// <summary>
    /// The ID of the activity instance associated with the bookmark.
    /// </summary>
    public string? ActivityInstanceId { get; set; }

    /// <summary>
    /// The type name of the activity associated with the bookmark.
    /// </summary>
    public string? ActivityTypeName { get; set; }

    /// <summary>
    /// Any options to apply when resuming the bookmark.
    /// </summary>
    public ResumeBookmarkOptions? Options { get; set; }

    /// <summary>
    /// The timestamp that this entity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Creates a <see cref="BookmarkFilter"/> from this bookmark queue item.
    /// </summary>
    public BookmarkFilter CreateBookmarkFilter()
    {
        return new()
        {
            WorkflowInstanceId = WorkflowInstanceId,
            CorrelationId = CorrelationId,
            BookmarkId = BookmarkId,
            Hash = StimulusHash,
            ActivityInstanceId = ActivityInstanceId,
            ActivityTypeName =  ActivityTypeName
        };
    }
}