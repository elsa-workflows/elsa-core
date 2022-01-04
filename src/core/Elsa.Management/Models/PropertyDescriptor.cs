namespace Elsa.Management.Models;

public abstract class PropertyDescriptor
{
    public string Name { get; set; } = default!;
    public Type Type { get; set; } = default!;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public float Order { get; set; }
    public bool? IsBrowsable { get; set; }
}