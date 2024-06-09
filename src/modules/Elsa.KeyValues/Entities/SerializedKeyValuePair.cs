using Elsa.Framework.Entities;

namespace Elsa.KeyValues.Entities;

/// <summary>
/// Represents a key-value pair with a serialized value.
/// </summary>
public class SerializedKeyValuePair : Entity
{
    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    public string Key => Id;

    /// <summary>
    /// Gets or sets the serialized value.
    /// </summary>
    public string SerializedValue { get; set; } = default!;
}