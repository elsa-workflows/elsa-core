using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Represents options for resuming a workflow host.
/// </summary>
public class ResumeWorkflowHostOptions
{
    /// <summary>An optional correlation ID.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>An optional bookmark ID.</summary>
    public string? BookmarkId { get; set; }

    /// <summary>An optional activity ID.</summary>
    public string? ActivityId { get; set; }

    /// <summary>An optional activity node ID.</summary>
    public string? ActivityNodeId { get; set; }

    /// <summary>An optional activity instance ID.</summary>
    public string? ActivityInstanceId { get; set; }

    /// <summary>An optional activity hash.</summary>
    public string? ActivityHash { get; set; }

    /// <summary>Optional input to pass to the workflow instance.</summary>
    public IDictionary<string, object>? Input { get; set; }
    
    /// <summary>
    /// Any properties to attach to the workflow instance.
    /// </summary>
    public IDictionary<string, object>? Properties { get; set; }

    /// <summary>Optional cancellation tokens that can be used to cancel the workflow instance without cancelling system-level operations.</summary>
    public CancellationTokens CancellationTokens { get; set; }
}