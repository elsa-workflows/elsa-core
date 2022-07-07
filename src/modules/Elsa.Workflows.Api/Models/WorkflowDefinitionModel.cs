using System;
using System.Collections.Generic;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Api.Models;

public record WorkflowDefinitionModel(
    string Id,
    string DefinitionId,
    string? Name,
    string? Description,
    DateTimeOffset CreatedAt,
    int Version,
    ICollection<VariableDefinition> Variables,
    IDictionary<string, object> Metadata,
    IDictionary<string, object> ApplicationProperties,
    bool IsLatest,
    bool IsPublished,
    IActivity Root
);