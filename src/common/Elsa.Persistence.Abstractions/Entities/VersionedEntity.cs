namespace Elsa.Persistence.Common.Entities;

public abstract class VersionedEntity : Entity
{
    public string LogicalId { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; } = 1;
    public bool IsLatest { get; set; }
    public bool IsPublished { get; set; }
}