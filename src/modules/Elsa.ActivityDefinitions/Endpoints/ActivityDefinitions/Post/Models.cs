using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Models;

namespace Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.Post;

public class Request
{
    public string DefinitionId { get; set; } = default!;
    public string TypeName { get; set; } = default!;
    public string? DisplayName { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public IActivity? Root { get; set; }
    public ICollection<VariableDefinition>? Variables { get; set; }
    public IDictionary<string, object>? Metadata { get; set; }
    public IDictionary<string, object>? ApplicationProperties { get; set; }
    public bool Publish { get; set; }
}

public class Response
{
    public Response()
    {
    }

    public Response(
        string id,
        string definitionId,
        string typeName,
        string? displayName,
        string? category,
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
        TypeName = typeName;
        DisplayName = displayName;
        Category = category;
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

    public string Id { get; set; } = default!;
    public string DefinitionId { get; set; } = default!;
    public string TypeName { get; set; } = default!;
    public string? DisplayName { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; }
    public ICollection<VariableDefinition> Variables { get; set; } = default!;
    public IDictionary<string, object> Metadata { get; set; } = default!;
    public IDictionary<string, object> ApplicationProperties { get; set; } = default!;
    public bool IsLatest { get; set; }
    public bool IsPublished { get; set; }
    public IActivity Root { get; set; } = default!;
}