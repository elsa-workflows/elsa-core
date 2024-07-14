using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime;

public class NewBookmarkQueueItem
{
    /// The workflow instance ID owning the bookmark to resume.
    public string? WorkflowInstanceId { get; set; } = default!;

    /// The bookmark ID to resume.
    public string? BookmarkId { get; set; } = default!;

    /// A bookmark payload hash of the bookmark to resume.
    public string? StimulusHash { get; set; }

    /// The ID of the activity instance associated with the bookmark.
    public string? ActivityInstanceId { get; set; }

    // The type name of the activity associated with the bookmark.
    public string? ActivityTypeName { get; set; }

    /// Any options to apply when resuming the bookmark.
    public ResumeBookmarkOptions? Options { get; set; }
}