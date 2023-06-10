using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Elsa.Api.Client.Resources.ActivityDescriptors.Models;

/// <summary>
/// A base type for <see cref="InputDescriptor"/> and <see cref="OutputDescriptor"/>.
/// </summary>
[PublicAPI]
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
    public string TypeName { get; set; } = default!;

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
}