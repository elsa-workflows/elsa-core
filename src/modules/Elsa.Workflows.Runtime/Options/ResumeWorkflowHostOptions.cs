using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Represents options for resuming a workflow host.
/// </summary>
/// <param name="CorrelationId">An optional correlation ID.</param>
/// <param name="BookmarkId">An optional bookmark ID.</param>
/// <param name="ActivityId">An optional activity ID.</param>
/// <param name="ActivityNodeId">An optional activity node ID.</param>
/// <param name="ActivityInstanceId">An optional activity instance ID.</param>
/// <param name="ActivityHash">An optional activity hash.</param>
/// <param name="Input">Optional input to pass to the workflow instance.</param>
/// <param name="CancellationTokens">Optional cancellation tokens that can be used to cancel the workflow instance without cancelling system-level operations.</param>
public record ResumeWorkflowHostOptions(
    string? CorrelationId = default,
    string? BookmarkId = default,
    string? ActivityId = default,
    string? ActivityNodeId = default,
    string? ActivityInstanceId = default,
    string? ActivityHash = default,
    IDictionary<string, object>? Input = default,
    CancellationTokens CancellationTokens = default);