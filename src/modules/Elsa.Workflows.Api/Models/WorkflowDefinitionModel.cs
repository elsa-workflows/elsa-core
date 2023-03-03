using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Services;
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
    IDictionary<string, object> Metadata,
    bool IsLatest,
    bool IsPublished,
    IActivity Root
);