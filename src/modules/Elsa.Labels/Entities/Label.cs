using Elsa.Common.Entities;

namespace Elsa.Labels.Entities;

/// <summary>
/// Represents an individual label.
/// </summary>
public class Label : Entity
{
    private string _name = default!;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            NormalizedName = value.ToLowerInvariant();
        }
    }

    public string NormalizedName { get; set; } = default!;
    public string? Description { get; set; }
    public string? Color { get; set; }
}