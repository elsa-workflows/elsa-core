using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Messages;

/// <summary>
/// A request to create a new workflow instance.
/// </summary>
[UsedImplicitly]
public class CreateWorkflowInstanceRequest
{
    /// The ID of the workflow definition version to create an instance of.
    public string DefinitionVersionId { get; set; } = default!;

    /// The correlation ID of the workflow, if any.
    public string? CorrelationId { get; set; }

    /// The input to the workflow instance, if any.
    public IDictionary<string, object>? Input { get; set; }

    /// Any properties to assign to the workflow instance.
    public IDictionary<string, object>? Properties { get; set; }

    /// The ID of the parent workflow instance, if any.
    public string? ParentId { get; set; }
}