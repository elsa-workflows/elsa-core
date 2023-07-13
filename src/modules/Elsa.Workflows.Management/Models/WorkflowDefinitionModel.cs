using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Models;

/// <summary>
/// Represents a serializable workflow definition.
/// </summary>
/// <param name="Id"></param>
/// <param name="DefinitionId"></param>
/// <param name="Name"></param>
/// <param name="Description"></param>
/// <param name="CreatedAt"></param>
/// <param name="Version"></param>
/// <param name="Variables"></param>
/// <param name="Inputs"></param>
/// <param name="Outputs"></param>
/// <param name="Outcomes"></param>
/// <param name="CustomProperties"></param>
/// <param name="UsableAsActivity"></param>
/// <param name="IsReadonly"></param>
/// <param name="IsLatest"></param>
/// <param name="IsPublished"></param>
/// <param name="Options">The type of <see cref="IWorkflowActivationStrategy"/> to apply when new instances are requested to be created.</param>
/// <param name="Root"></param>
[PublicAPI]
public record WorkflowDefinitionModel(
    string Id,
    string DefinitionId,
    string? Name,
    string? Description,
    DateTimeOffset CreatedAt,
    int Version,
    Version? ToolVersion,
    ICollection<VariableDefinition>? Variables,
    ICollection<InputDefinition>? Inputs,
    ICollection<OutputDefinition>? Outputs,
    ICollection<string>? Outcomes,
    IDictionary<string, object>? CustomProperties,
    bool IsReadonly,
    bool IsLatest,
    bool IsPublished,
    WorkflowOptions? Options,
    [property: Obsolete("Use Options.UsableAsActivity instead")]
    bool? UsableAsActivity,
    IActivity? Root
)
{
    /// <inheritdoc />
    public WorkflowDefinitionModel() : this(
        default!,
        default!,
        default!,
        default!,
        default!,
        default!,
        default!,
        default!,
        default!,
        default!,
        default!,
        default!,
        default!,
        default!,
        default!,
        default!,
        default!,
        default!)
    {
    }
}