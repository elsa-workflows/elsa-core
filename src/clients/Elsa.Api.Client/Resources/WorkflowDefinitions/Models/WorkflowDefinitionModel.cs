using Elsa.Api.Client.Activities;
using JetBrains.Annotations;

namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

/// <summary>
/// Represents a serializable workflow definition.
/// </summary>
[PublicAPI]
public class WorkflowDefinitionModel
{
    public string Id { get; set; } = default!;
    public string DefinitionId { get; set; } = default!;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; }
    public Version? ToolVersion { get; set; }
    public ICollection<VariableDefinition>? Variables { get; set; }
    public ICollection<InputDefinition>? Inputs { get; set; }
    public ICollection<OutputDefinition>? Outputs { get; set; }
    public ICollection<string>? Outcomes { get; set; }
    public IDictionary<string, object>? CustomProperties { get; set; }
    public bool IsReadonly { get; set; }
    public bool IsLatest { get; set; }
    public bool IsPublished { get; set; }

    /// <summary>The type of <c>IWorkflowActivationStrategy</c> to apply when new instances are requested to be created.</summary>
    public WorkflowOptions? Options { get; set; }

    /// <summary></summary>
    public Activity? Root { get; set; }
}