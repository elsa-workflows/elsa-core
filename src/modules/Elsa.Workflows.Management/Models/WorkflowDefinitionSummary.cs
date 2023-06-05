using Elsa.Workflows.Management.Entities;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Models;

/// <summary>
/// A summary of a workflow definition.
/// </summary>
[PublicAPI]
public class WorkflowDefinitionSummary
{
    /// <summary>
    /// Creates a new instance of the <see cref="WorkflowDefinitionSummary"/> class from the specified <see cref="WorkflowDefinition"/> instance.
    /// </summary>
    public static WorkflowDefinitionSummary FromDefinition(WorkflowDefinition workflowDefinition) => new()
    {
        Id = workflowDefinition.Id,
        DefinitionId = workflowDefinition.DefinitionId,
        Name = workflowDefinition.Name,
        Description = workflowDefinition.Description,
        Version = workflowDefinition.Version,
        IsLatest = workflowDefinition.IsLatest,
        IsPublished = workflowDefinition.IsPublished,
        ProviderName = workflowDefinition.ProviderName,
        MaterializerName = workflowDefinition.MaterializerName,
        CreatedAt = workflowDefinition.CreatedAt
    };

    public string Id { get; init; }
    public string DefinitionId { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public int? Version { get; init; }
    public bool IsLatest { get; init; }
    public bool IsPublished { get; init; }
    public string? ProviderName { get; init; }
    public string MaterializerName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}