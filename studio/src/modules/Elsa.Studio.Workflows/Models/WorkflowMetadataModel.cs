using System.ComponentModel.DataAnnotations;

namespace Elsa.Studio.Workflows.Models;

/// <summary>
/// Represents the metadata of a workflow.
/// </summary>
public class WorkflowMetadataModel
{
    /// <summary>
    /// Gets or sets the ID of the workflow definition.
    /// </summary>
    public string? DefinitionId { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the workflow.
    /// </summary>
    [Required] public string? Name { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the workflow.
    /// </summary>
    public string? Description { get; set; } = null!;
}