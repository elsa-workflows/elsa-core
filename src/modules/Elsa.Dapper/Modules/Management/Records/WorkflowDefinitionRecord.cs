namespace Elsa.Dapper.Modules.Management.Records;

internal class WorkflowDefinitionRecord
{
    public string Id { get; set; } = default!;
    public string DefinitionId { get; set; } = default!;
    public string? Name { get; set; }
    public string? ToolVersion { get; set; }
    public string? Description { get; set; }
    public string? ProviderName { get; set; }
    public string MaterializerName { get; set; } = default!;
    public string? MaterializerContext { get; set; }
    public string Props { get; set; } = default!;
    public bool? UsableAsActivity { get; set; }
    public string? StringData { get; set; }
    public byte[]? BinaryData { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; } = 1;
    public bool IsLatest { get; set; }
    public bool IsPublished { get; set; }
    public bool IsReadonly { get; set; } = false;
}