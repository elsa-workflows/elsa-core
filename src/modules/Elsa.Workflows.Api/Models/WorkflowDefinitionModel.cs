using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Api.Models;

internal record WorkflowDefinitionModel(
    string Id,
    string DefinitionId,
    string? Name,
    string? Description,
    DateTimeOffset CreatedAt,
    int Version,
    ICollection<VariableDefinition> Variables,
    ICollection<InputDefinition> Inputs,
    ICollection<OutputDefinition> Outputs,
    ICollection<string> Outcomes,
    IDictionary<string, object> Metadata,
    bool? UsableAsActivity,
    bool IsLatest,
    bool IsPublished,
    IActivity Root
);