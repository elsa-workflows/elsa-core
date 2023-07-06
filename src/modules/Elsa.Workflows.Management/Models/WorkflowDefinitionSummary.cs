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
        ToolVersion = workflowDefinition.ToolVersion,
        IsLatest = workflowDefinition.IsLatest,
        IsPublished = workflowDefinition.IsPublished,
        ProviderName = workflowDefinition.ProviderName,
        MaterializerName = workflowDefinition.MaterializerName,
        CreatedAt = workflowDefinition.CreatedAt
    };

    /// <summary>
    /// The version ID of the workflow definition.
    /// </summary>
    public string Id { get; set; } = default!;
    
    /// <summary>
    /// The ID of the workflow definition.
    /// </summary>
    public string DefinitionId { get; set; } = default!;
    
    /// <summary>
    /// The name of the workflow definition.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// The description of the workflow definition.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// The version of the workflow definition.
    /// </summary>
    public int? Version { get; set; }
    
    /// <summary>
    /// The version of the tool that created the workflow definition.
    /// </summary>
    public Version? ToolVersion { get; set; }
    
    /// <summary>
    /// Whether this is the latest version of the workflow definition.
    /// </summary>
    public bool IsLatest { get; set; }
    
    /// <summary>
    /// Whether this workflow definition is published.
    /// </summary>
    public bool IsPublished { get; set; }
    
    /// <summary>
    /// The provider name of the workflow definition.
    /// </summary>
    public string? ProviderName { get; set; }
    
    /// <summary>
    /// The materializer name of the workflow definition.
    /// </summary>
    public string MaterializerName { get; set; }  = default!;
    
    /// <summary>
    /// The timestamp when the workflow definition was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Makes workflow
    /// </summary>
    public bool IsReadonly { get; set; }
}