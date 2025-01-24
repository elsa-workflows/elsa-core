using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime;

public class NewBookmarkQueueItem
{
    /// <summary>
    /// The workflow instance ID owning the bookmark to resume.
    /// </summary>
    public string? WorkflowInstanceId { get; set; } = null!;
    
    /// <summary>
    /// The correlation ID associated with the workflow instance owning the bookmark to resume.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The bookmark ID to resume.
    /// </summary>
    public string? BookmarkId { get; set; } = null!;

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