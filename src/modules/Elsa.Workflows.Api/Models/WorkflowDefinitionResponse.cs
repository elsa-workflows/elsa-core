using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Api.Models;

public class WorkflowDefinitionResponse
{
    public WorkflowDefinitionResponse(
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

    public string Id { get; }
    public string DefinitionId { get; }
    public string? Name { get; }
    public string? Description { get; }
    public DateTimeOffset CreatedAt { get; }
    public int Version { get; }
    public ICollection<VariableDefinition> Variables { get; }
    public IDictionary<string, object> Metadata { get; }
    public IDictionary<string, object> ApplicationProperties { get; }
    public bool IsLatest { get; }
    public bool IsPublished { get; }
    public IActivity Root { get; }
}