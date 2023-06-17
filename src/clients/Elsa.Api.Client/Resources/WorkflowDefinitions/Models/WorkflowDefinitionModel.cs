using System.Text.Json;
using JetBrains.Annotations;

namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

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
/// <param name="IsReadonly"></param>
/// <param name="IsLatest"></param>
/// <param name="IsPublished"></param>
/// <param name="Options">The type of <c>IWorkflowActivationStrategy</c> to apply when new instances are requested to be created.</param>
/// <param name="Root"></param>
[PublicAPI]
public record WorkflowDefinitionModel(
    string Id,
    string DefinitionId,
    string? Name,
    string? Description,
    DateTimeOffset CreatedAt,
    int Version,
    ICollection<VariableDefinition>? Variables,
    ICollection<InputDefinition>? Inputs,
    ICollection<OutputDefinition>? Outputs,
    ICollection<string>? Outcomes,
    IDictionary<string, object>? CustomProperties,
    bool IsReadonly,
    bool IsLatest,
    bool IsPublished,
    WorkflowOptions? Options,
    JsonElement? Root
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
        default!)
    {
    }
}