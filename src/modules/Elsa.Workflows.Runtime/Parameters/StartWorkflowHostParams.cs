namespace Elsa.Workflows.Runtime.Parameters;

/// Represents options for starting a workflow host.
public class StartWorkflowHostParams
{
    /// An optional workflow instance ID. If not specified, a new ID will be generated.
    public string? InstanceId { get; set; }

    /// Indicates whether the workflow instance to start is a new instance or an existing one.
    public bool IsExistingInstance { get; set; }

    /// An optional correlation ID.
    public string? CorrelationId { get; set; }

    /// Optional input to pass to the workflow instance.
    public IDictionary<string, object>? Input { get; set; }

    /// Any properties to attach to the workflow instance.
    public IDictionary<string, object>? Properties { get; set; }

    /// The ID of the activity that triggered the workflow instance.
    public string? TriggerActivityId { get; set; }

    /// <summary>Cancellation tokens that can be used to cancel the workflow instance without cancelling system-level operations.</summary>
    public CancellationToken CancellationToken { get; set; }

    /// Callback method that will be called when the status of the workflow has been updated
    public Action<WorkflowExecutionContext>? StatusUpdatedCallback { get; set; }

    /// The ID of the parent workflow instance.
    public string? ParentWorkflowInstanceId { get; set; }
}