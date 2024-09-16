namespace Elsa.Common.Entities;

/// An entity that maintains a "created" timestamp. 
public abstract class ManagedEntity : Entity
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? Owner { get; set; }
}