using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// A descriptor of an activity type. It also provides a constructor to create instances of this type.
/// </summary>
[DebuggerDisplay("{TypeName}")]
public class ActivityDescriptor
{
    public string TypeName { get; set; } = default!;
    public int Version { get; set; }
    public string Category { get; set; } = default!;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public ICollection<InputDescriptor> Inputs { get; init; } = new List<InputDescriptor>();
    public ICollection<OutputDescriptor> Outputs { get; init; } = new List<OutputDescriptor>();
    [JsonIgnore] public ICollection<Attribute> Attributes { get; set; } = new List<Attribute>();

    /// <summary>
    /// Instantiates a concrete instance of an <see cref="IActivity"/>.
    /// </summary>
    [JsonIgnore]
    public Func<ActivityConstructorContext, IActivity> Constructor { get; init; } = default!;

    public ActivityKind Kind { get; set; } = ActivityKind.Action;
    public ICollection<Port> Ports { get; init; } = new List<Port>();
    public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();

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