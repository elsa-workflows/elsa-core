namespace Elsa.Persistence.VNext.Runtime.Models;

public class RuntimeEntityDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = default!;
    public int Version { get; set; } = 1;
    public RuntimeEntityDefinitionStatus Status { get; set; } = RuntimeEntityDefinitionStatus.Draft;
    public List<RuntimeEntityFieldDefinition> Fields { get; set; } = [];
    public List<RuntimeEntityIndexDefinition> Indexes { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
