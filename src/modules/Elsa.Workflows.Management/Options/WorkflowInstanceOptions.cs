using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Options;

/// <summary>
/// Represents parameters for creating a workflow instance.
/// </summary>
[UsedImplicitly]
public class WorkflowInstanceOptions
{
    /// <summary>
    /// The correlation ID of the workflow, if any.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The input to the workflow instance, if any.
    /// </summary>
    public IDictionary<string, object>? Input { get; set; }

    /// <summary>
    /// Any properties to assign to the workflow instance.
    /// </summary>
    public IDictionary<string, object>? Properties { get; set; }

    /// <summary>
    /// A pre-created workflow instance ID that the new workflow instance should use. If not provided, a new ID will be generated.
    /// </summary>
    public string? WorkflowInstanceId { get; set; }

    /// <summary>
    /// The ID of the parent workflow instance, if any.
    /// </summary>
    public string? ParentWorkflowInstanceId { get; set; }
}