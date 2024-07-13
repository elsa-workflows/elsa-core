using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime;

public class NewBookmarkQueueItem
{
    /// The workflow instance ID owning the bookmark to resume.
    public string WorkflowInstanceId { get; set; } = default!;
    
    /// The bookmark ID to resume.
    public string? BookmarkId { get; set; } = default!;
    
    /// A bookmark payload hash of the bookmark to resume.
    public string? BookmarkHash { get; set; }
    
    /// Any options to apply when resuming the bookmark.
    public ResumeBookmarkOptions? Options { get; set; }
}