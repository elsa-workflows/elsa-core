using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Options for resuming workflows.
/// </summary>
public record ResumeWorkflowRuntimeOptions(
    string? CorrelationId = default,
    string? BookmarkId = default, 
    string? ActivityId = default,
    string? ActivityNodeId = default,
    string? ActivityInstanceId = default,
    string? ActivityHash = default,
    IDictionary<string, object>? Input = default,
    CancellationTokens CancellationTokens = default);