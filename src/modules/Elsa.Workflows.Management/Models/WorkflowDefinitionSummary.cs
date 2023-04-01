using Elsa.Workflows.Management.Entities;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Models;

/// <summary>
/// A summary of a workflow definition.
/// </summary>
[PublicAPI]
public record WorkflowDefinitionSummary(string Id, string DefinitionId, string? Name, string? Description, int? Version, bool IsLatest, bool IsPublished, string MaterializerName, DateTimeOffset CreatedAt)
{
    /// <summary>
    /// Creates a new instance of the <see cref="WorkflowDefinitionSummary"/> class from the specified <see cref="WorkflowDefinition"/> instance.
    /// </summary>
    public static WorkflowDefinitionSummary FromDefinition(WorkflowDefinition workflowDefinition) => new(
        workflowDefinition.Id,
        workflowDefinition.DefinitionId,
        workflowDefinition.Name,
        workflowDefinition.Description,
        workflowDefinition.Version,
        workflowDefinition.IsLatest,
        workflowDefinition.IsPublished,
        workflowDefinition.MaterializerName,
        workflowDefinition.CreatedAt
    );
}