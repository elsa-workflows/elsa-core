namespace Elsa.Persistence.VNext.Runtime.Models;

public class RuntimeEntityInstance
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DefinitionName { get; set; } = default!;
    public int DefinitionVersion { get; set; }
    public Dictionary<string, object?> Data { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
