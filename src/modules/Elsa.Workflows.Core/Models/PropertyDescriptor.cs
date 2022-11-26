using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.Models;

public abstract class PropertyDescriptor
{
    public string Name { get; set; } = default!;
    [JsonPropertyName("typeName")]public Type Type { get; set; } = default!;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public float Order { get; set; }
    public bool? IsBrowsable { get; set; }
}