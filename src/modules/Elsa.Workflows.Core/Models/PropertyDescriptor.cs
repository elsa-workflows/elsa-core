using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Models;

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
    /// True if this input should be displayed by UI tools, false otherwise.
    /// </summary>
    public bool? IsBrowsable { get; set; }

    /// <summary>
    /// True if this input property is synthetic, which means it does not exist physically on the activity's .NET type.
    /// </summary>
    public bool IsSynthetic { get; set; }
    
    /// <summary>
    /// Returns the value of the input property for the specified activity.
    /// </summary>
    [JsonIgnore]
    public Func<IActivity, object?> ValueGetter { get; set; } = default!;
}