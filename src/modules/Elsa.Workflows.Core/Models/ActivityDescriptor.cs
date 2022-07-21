using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// A descriptor of an activity type. It also provides a constructor to create instances of this type.
/// </summary>
public class ActivityDescriptor
{
    public string ActivityType { get; init; } = default!;
    public string Category { get; init; } = default!;
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public ICollection<InputDescriptor> Inputs { get; init; } = new List<InputDescriptor>();
    public ICollection<OutputDescriptor> Outputs { get; init; } = new List<OutputDescriptor>();
    [JsonIgnore] public Func<ActivityConstructorContext, IActivity> Constructor { get; init; } = default!;
    public ActivityKind Kind { get; set; } = ActivityKind.Action;
    public ICollection<Port> Ports { get; init; } = new List<Port>();
    
    /// <summary>
    /// A value indicating whether this activity is a container of child activities.
    /// </summary>
    public bool IsContainer { get; set; }

    /// <summary>
    /// Whether this activity type is selectable from activity pickers.
    /// </summary>
    public bool IsBrowsable { get; set; }
}

public record ActivityConstructorContext(JsonElement Element, JsonSerializerOptions SerializerOptions);