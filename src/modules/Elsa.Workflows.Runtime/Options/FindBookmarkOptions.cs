namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Options for triggering workflows.
/// </summary>
public class FindBookmarkOptions
{
    public string? BookmarkId { get; set; }
    public string? CorrelationId { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public string? ActivityInstanceId { get; set; }
}