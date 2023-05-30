using Elsa.MongoDB.Common;

namespace Elsa.MongoDB.Models;

public class WorkflowDefinition : MongoDocument
{
    public string DefinitionId { get; set; } = default!;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string MaterializerName { get; set; } = default!;
    public string? MaterializerContext { get; set; }
    public string? StringData { get; set; }
    public byte[]? BinaryData { get; set; }
    public bool? UsableAsActivity { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; } = 1;
    public bool IsLatest { get; set; }
    public bool IsPublished { get; set; }
    public string Data { get; set; } = default!;
}