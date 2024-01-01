using System.Reflection;
using System.Text.Json.Serialization;
using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.Models;

/// <summary>
/// A base type for <see cref="InputDescriptor"/> and <see cref="OutputDescriptor"/>.
/// </summary>
public abstract class PropertyDescriptor
{
    /// <summary>
    /// The name.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// The .NET type.
    /// </summary>
    [JsonPropertyName("typeName")]
    public Type Type { get; set; } = default!;

    /// <summary>
    /// The user friendly name of the input. Used by UI tools.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The user friendly description of the input. Used by UI tools.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The order in which this input should be displayed by UI tools.
    /// </summary>
    public float Order { get; set; }

    /// <summary>
    /// True if this property should be displayed by UI tools, false otherwise.
    /// </summary>
    public bool? IsBrowsable { get; set; } = true;
    
    /// <summary>
    /// True if this property can be serialized.
    /// </summary>
    public bool? IsSerializable { get; set; }

    /// <summary>
    /// True if this input property is synthetic, which means it does not exist physically on the activity's .NET type.
    /// </summary>
    public bool IsSynthetic { get; set; }
    
    /// <summary>
    /// Returns the value of the input property for the specified activity.
    /// </summary>
    [JsonIgnore]
    public Func<IActivity, object?> ValueGetter { get; set; } = default!;
    
    /// <summary>
    /// Sets the value of the input property for the specified activity.
    /// </summary>
    [JsonIgnore]
    public Action<IActivity, object?> ValueSetter { get; set; } = default!;
    
    /// <summary>
    /// The source of the property, if any.
    /// </summary>
    [JsonIgnore]
    public PropertyInfo? PropertyInfo { get; set; }
}