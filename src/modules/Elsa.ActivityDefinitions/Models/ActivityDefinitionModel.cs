using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Models;

namespace Elsa.ActivityDefinitions.Models;

public class ActivityDefinitionModel
{
    public ActivityDefinitionModel()
    {
    }

    public ActivityDefinitionModel(
        string id,
        string definitionId,
        string? name,
        string? description,
        DateTimeOffset createdAt,
        int version,
        ICollection<VariableDefinition> variables,
        IDictionary<string, object> metadata,
        IDictionary<string, object> applicationProperties,
        bool isLatest,
        bool isPublished,
        IActivity root)
    {
        Id = id;
        DefinitionId = definitionId;
        Name = name;
        Description = description;
        CreatedAt = createdAt;
        Version = version;
        Variables = variables;
        Metadata = metadata;
        ApplicationProperties = applicationProperties;
        IsLatest = isLatest;
        IsPublished = isPublished;
        Root = root;
    }

    public string Id { get; init; } = default!;
    public string DefinitionId { get; init; } = default!;
    public string? Name { get; init; }
    public string? Description { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public int Version { get; init; }
    public ICollection<VariableDefinition> Variables { get; init; } = default!;
    public IDictionary<string, object> Metadata { get; init; } = default!;
    public IDictionary<string, object> ApplicationProperties { get; init; } = default!;
    public bool IsLatest { get; init; }
    public bool IsPublished { get; init; }
    public IActivity Root { get; init; } = default!;
}