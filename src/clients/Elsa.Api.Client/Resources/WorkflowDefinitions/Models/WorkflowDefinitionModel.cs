using System.Text.Json.Nodes;
using Elsa.Api.Client.Shared.Models;
using JetBrains.Annotations;

namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

/// <summary>
/// Represents a serializable workflow definition.
/// </summary>
[PublicAPI]
public class WorkflowDefinitionModel : LinkedEntity
{
    /// <summary>
    /// Gets or sets the definition ID of the workflow definition.
    /// </summary>
    public string DefinitionId { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the name of the workflow definition.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the workflow definition.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the version of the tool that created the workflow definition.
    /// </summary>
    public Version? ToolVersion { get; set; }
    
    /// <summary>
    /// Gets or sets the variables of the workflow definition.
    /// </summary>
    public ICollection<VariableDefinition>? Variables { get; set; }
    
    /// <summary>
    /// Gets or sets the inputs of the workflow definition.
    /// </summary>
    public ICollection<InputDefinition>? Inputs { get; set; }
    
    /// <summary>
    /// Gets or sets the outputs of the workflow definition.
    /// </summary>
    public ICollection<OutputDefinition>? Outputs { get; set; }
    
    /// <summary>
    /// Gets or sets the outcomes of the workflow definition.
    /// </summary>
    public ICollection<string>? Outcomes { get; set; }
    
    /// <summary>
    /// Gets or sets the custom properties associated with the workflow definition.
    /// </summary>
    public IDictionary<string, object>? CustomProperties { get; set; }
    
    /// <summary>
    /// Gets or sets whether the workflow definition is read-only.
    /// </summary>
    public bool IsReadonly { get; set; }
    
    /// <summary>
    /// Gets or sets the <see cref="WorkflowOptions"/> of the workflow definition.
    /// </summary>
    public WorkflowOptions? Options { get; set; }

    /// <summary>
    /// Gets or sets the root activity of the workflow definition.
    /// </summary>
    public JsonObject? Root { get; set; }
}