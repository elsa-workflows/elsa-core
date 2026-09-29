using Elsa.Persistence.Dapper.Records;

namespace Elsa.Persistence.Dapper.Modules.Management.Records;

internal class WorkflowDefinitionRecord : Record
{
    public string DefinitionId { get; set; } = null!;
    public string? Name { get; set; }
    public string? ToolVersion { get; set; }
    public string? Description { get; set; }
    public string? ProviderName { get; set; }
    public string MaterializerName { get; set; } = null!;
    public string? MaterializerContext { get; set; }
    public string Props { get; set; } = null!;
    public bool? UsableAsActivity { get; set; }
    public string? StringData { get; set; }
    public byte[]? BinaryData { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; } = 1;
    public bool IsLatest { get; set; }
    public bool IsPublished { get; set; }
    public bool IsReadonly { get; set; }
    public bool IsSystem { get; set; }
}