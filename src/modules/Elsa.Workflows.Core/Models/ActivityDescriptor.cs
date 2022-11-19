using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// A descriptor of an activity type. It also provides a constructor to create instances of this type.
/// </summary>
[DebuggerDisplay("{Type}")]
public class ActivityDescriptor
{
    public string Type { get; init; } = default!;
    public int Version { get; init; }
    public string Category { get; init; } = default!;
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public ICollection<InputDescriptor> Inputs { get; init; } = new List<InputDescriptor>();
    public ICollection<OutputDescriptor> Outputs { get; init; } = new List<OutputDescriptor>();
    
    /// <summary>
    /// The concrete type that this descriptor instantiates via the <see cref="Constructor"/> factory.
    /// </summary>
    public Type ActivityType { get; set; } = default!;
    
    /// <summary>
    /// Instantiates a concrete instance of an <see cref="IActivity"/>.
    /// </summary>
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