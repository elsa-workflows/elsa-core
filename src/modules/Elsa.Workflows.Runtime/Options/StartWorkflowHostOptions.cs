using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Represents options for starting a workflow host.
/// </summary>
public class StartWorkflowHostOptions
{
    /// <summary>An optional workflow instance ID. If not specified, a new ID will be generated.</summary>
    public string? InstanceId { get; set; }

    /// <summary>An optional correlation ID.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Optional input to pass to the workflow instance.</summary>
    public IDictionary<string, object>? Input { get; set; }

    /// <summary>Any properties to attach to the workflow instance.</summary>
    public IDictionary<string, object>? Properties { get; set; }

    /// <summary>The ID of the activity that triggered the workflow instance.</summary>
    public string? TriggerActivityId { get; set; }

    /// <summary>Cancellation tokens that can be used to cancel the workflow instance without cancelling system-level operations.</summary>
    public CancellationTokens CancellationTokens { get; set; }

    /// <summary>
    /// Callback method that will be called when the status of the workflow has been updated
    /// </summary>
    public Action<WorkflowExecutionContext>? StatusUpdatedCallback { get; set; }
}