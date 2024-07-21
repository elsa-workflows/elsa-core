using Elsa.Common.Models;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Models;

/// Represents a serializable workflow definition.
[PublicAPI]
public record WorkflowDefinitionModel(
    string Id,
    string DefinitionId,
    string? TenantId,
    string? Name,
    string? Description,
    DateTimeOffset CreatedAt,
    int Version,
    Version? ToolVersion,
    ICollection<VariableDefinition>? Variables,
    ICollection<InputDefinition>? Inputs,
    ICollection<OutputDefinition>? Outputs,
    ICollection<string>? Outcomes,
    [property: Obsolete("Use PropertyBag instead")]
    IDictionary<string, object>? CustomProperties,
    PropertyBag? PropertyBag,
    bool IsReadonly,
    bool IsSystem,
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
        default,
        default,
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