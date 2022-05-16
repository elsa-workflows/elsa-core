using System;
using System.Collections.Generic;
using Elsa.Models;
using Elsa.Services;

namespace Elsa.Api.Models;

public record WorkflowDefinitionModel(
    string Id,
    string DefinitionId,
    string? Name,
    string? Description,
    DateTimeOffset CreatedAt,
    int Version,
    ICollection<Variable> Variables,
    IDictionary<string, object> Metadata,
    IDictionary<string, object> ApplicationProperties,
    bool IsLatest,
    bool IsPublished,
    ICollection<string> Tags,
    IActivity Root
);