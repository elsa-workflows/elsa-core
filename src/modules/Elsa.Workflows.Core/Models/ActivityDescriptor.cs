using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Workflows.Models;

/// <summary>
/// A descriptor of an activity type. It also provides a constructor to create instances of this type.
/// </summary>
[DebuggerDisplay("{TypeName}")]
public class ActivityDescriptor
{
    public string? TenantId { get; set; } // Null means tenant-agnostic.
    
    /// <summary>
    /// The fully qualified name of the activity type.
    /// </summary>
    public string TypeName { get; set; } = null!;

    /// <summary>
    /// The .NET type of the activity type.
    /// </summary>
    public Type ClrType { get; set; } = null!;

    /// <summary>
    /// The namespace of the activity type.
    /// </summary>
    public string Namespace { get; set; } = null!;
    
    /// <summary>
    /// The name of the activity type.
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// The version of the activity type.
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// The category of the activity type.
    /// </summary>
    public string Category { get; set; } = null!;
    
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
    public Func<ActivityConstructorContext, ActivityConstructionResult> Constructor { get; set; } = null!;

    /// <summary>
    /// The kind of activity.
    /// </summary>
    public ActivityKind Kind { get; set; } = ActivityKind.Action;
    
    /// <summary>
    /// Whether the activity should be executed asynchronously. Applies only when the Kind is set to Task and as a default when not set on the activity itself.
    /// </summary>
    public bool RunAsynchronously { get; set; }
    
    /// <summary>
    /// The ports of the activity type.
    /// </summary>
    public ICollection<Port> Ports { get; set; } = new List<Port>();
    
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

    /// <summary>
    /// Whether this activity type is a start activity.
    /// </summary>
    public bool IsStart { get; set; }
    
    /// <summary>
    /// Whether this activity type is a terminal activity.
    /// </summary>
    public bool IsTerminal { get; set; }

    /// <summary>
    /// Gets or sets a function that allows configuring the JsonSerializerOptions for the activity during serialization.
    /// </summary>
    /// <remarks>
    /// This function can be used to customize the serialization options for an activity. It receives a JsonSerializerOptions
    /// object as an argument and should return the modified JsonSerializerOptions.
    /// <para>Example:</para>
    /// <code>
    /// activityDescriptor.ConfigureSerializerOptions = options =>
    /// {
    /// options.Converters.Add(new JsonIgnoreCompositeRootConverterFactory());
    /// return options;
    /// };
    /// </code>
    /// </remarks>
    [JsonIgnore]
    public Func<JsonSerializerOptions, JsonSerializerOptions>? ConfigureSerializerOptions { get; set; }
}