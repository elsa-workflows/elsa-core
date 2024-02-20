namespace Elsa.KeyValues.Entities;

/// <summary>
/// Represents a key-value pair with a serialized value.
/// </summary>
public class SerializedKeyValuePair
{
    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    public string Key { get; set; } = default!;

    /// <summary>
    /// Gets or sets the serialized value.
    /// </summary>
    public string SerializedValue { get; set; } = default!;
}