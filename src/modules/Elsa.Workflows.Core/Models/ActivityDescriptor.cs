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
    /// <summary>
    /// The fully qualified name of the activity type.
    /// </summary>
    public string TypeName { get; set; } = default!;

    /// <summary>
    /// The namespace of the activity type.
    /// </summary>
    public string Namespace { get; set; } = default!;
    
    /// <summary>
    /// The name of the activity type.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// The version of the activity type.
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// The category of the activity type.
    /// </summary>
    public string Category { get; set; } = default!;
    
    /// <summary>
    /// The display name of the activity type.
    /// </summary>
    public string? DisplayName { get; set; }
    
    /// <summary>
    /// The description of the activity type.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// The input properties of the activity type.
    /// </summary>
    public ICollection<InputDescriptor> Inputs { get; init; } = new List<InputDescriptor>();
    
    /// <summary>
    /// The output properties of the activity type.
    /// </summary>
    public ICollection<OutputDescriptor> Outputs { get; init; } = new List<OutputDescriptor>();
    
    /// <summary>
    /// The attributes of the activity type.
    /// </summary>
    [JsonIgnore] public ICollection<Attribute> Attributes { get; set; } = new List<Attribute>();

    /// <summary>
    /// Instantiates a concrete instance of an <see cref="IActivity"/>.
    /// </summary>
    [JsonIgnore]
    public Func<ActivityConstructorContext, IActivity> Constructor { get; init; } = default!;

    /// <summary>
    /// The kind of activity.
    /// </summary>
    public ActivityKind Kind { get; set; } = ActivityKind.Action;
    
    /// <summary>
    /// The ports of the activity type.
    /// </summary>
    public ICollection<Port> Ports { get; init; } = new List<Port>();
    
    /// <summary>
    /// The custom properties of the activity type.
    /// </summary>
    public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
    
    /// <summary>
    /// The properties to set when constructing an activity in the designer.
    /// </summary>
    public IDictionary<string, object> ConstructionProperties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// A value indicating whether this activity is a container of child activities.
    /// </summary>
    public bool IsContainer { get; set; }

    /// <summary>
    /// Whether this activity type is selectable from activity pickers.
    /// </summary>
    public bool IsBrowsable { get; set; } = true;
}

// TODO: Refactor this to remove the dependency on JsonElement and JsonSerializerOptions.
// This limits the ability to use this class in other contexts, such as constructing activities from the DSL.
public record ActivityConstructorContext(ActivityDescriptor ActivityDescriptor, JsonElement Element, JsonSerializerOptions SerializerOptions);