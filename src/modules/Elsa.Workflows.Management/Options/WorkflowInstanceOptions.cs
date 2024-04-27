using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Options;

/// Represents parameters for creating a workflow instance.
[UsedImplicitly]
public class WorkflowInstanceOptions
{
    /// The correlation ID of the workflow, if any.
    public string? CorrelationId { get; set; }

    /// The input to the workflow instance, if any.
    public IDictionary<string, object>? Input { get; set; }

    /// Any properties to assign to the workflow instance.
    public IDictionary<string, object>? Properties { get; set; }

    /// A pre-created workflow instance ID that the new workflow instance should use. If not provided, a new ID will be generated.
    public string? WorkflowInstanceId { get; set; }

    /// The ID of the parent workflow instance, if any.
    public string? ParentWorkflowInstanceId { get; set; }
}