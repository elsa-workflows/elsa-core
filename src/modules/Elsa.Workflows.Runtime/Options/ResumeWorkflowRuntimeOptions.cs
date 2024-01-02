using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Options for resuming workflows.
/// </summary>
public class ResumeWorkflowRuntimeOptions
{
    public string? CorrelationId { get; set; }
    public string? BookmarkId { get; set; }
    public string? ActivityId { get; set; }
    public string? ActivityNodeId { get; set; }
    public string? ActivityInstanceId { get; set; }
    public string? ActivityHash { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
    public CancellationTokens CancellationTokens { get; set; }
}