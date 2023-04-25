using Elsa.Workflows.Core.Contracts;

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
/// <param name="IsLatest"></param>
/// <param name="IsPublished"></param>
/// <param name="Root"></param>
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
    bool? UsableAsActivity,
    bool IsLatest,
    bool IsPublished,
    IActivity? Root
);