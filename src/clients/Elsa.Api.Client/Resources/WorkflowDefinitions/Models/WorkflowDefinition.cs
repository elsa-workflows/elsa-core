using System.Text.Json.Nodes;
using Elsa.Api.Client.Shared.Models;

namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

/// <summary>
/// Represents a versioned workflow definition.
/// </summary>
public class WorkflowDefinition : VersionedEntity
{
    /// <summary>
    /// The logical ID of the workflow. This ID is the same across versions. 
    /// </summary>
    public string DefinitionId { get; set; } = default!;

    /// <summary>
    /// The name of the workflow.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// A short description of that the workflow is about.  
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The version of the tool that created this workflow.
    /// </summary>
    public Version? ToolVersion { get; set; }

    /// <summary>
    /// A set of options for the workflow.
    /// </summary>
    public WorkflowOptions Options { get; set; } = new();

    /// <summary>
    /// A set of workflow variables that are accessible throughout the workflow.
    /// </summary>
    public ICollection<Variable> Variables { get; set; } = new List<Variable>();

    /// <summary>
    /// A set of input definitions.
    /// </summary>
    public ICollection<InputDefinition> Inputs { get; set; } = new List<InputDefinition>();

    /// <summary>
    /// A set of output definitions.
    /// </summary>
    public ICollection<OutputDefinition> Outputs { get; set; } = new List<OutputDefinition>();

    /// <summary>
    /// A set of possible outcomes for this workflow.
    /// </summary>
    public ICollection<string> Outcomes { get; set; } = new List<string>();

    /// <summary>
    /// Stores custom information about the workflow. Can be used to store application-specific properties to associate with the workflow.
    /// </summary>
    public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// The name of the workflow provider that created this workflow, if any.
    /// </summary>
    public string? ProviderName { get; set; }

    /// <summary>
    /// The name of the workflow materializer to interpret the <c>StringData</c> or <c>BinaryData</c>.
    /// </summary>
    public string MaterializerName { get; set; } = default!;

    /// <summary>
    /// Provider-specific data.
    /// </summary>
    public string? MaterializerContext { get; set; }

    /// <summary>
    /// The root activity of the workflow.
    /// </summary>
    public JsonObject Root { get; set; } = default!;
    
    /// <summary>
    /// An option to use the workflow as a readonly workflow.
    /// </summary>
    public bool IsReadonly { get; set; } = false;
}