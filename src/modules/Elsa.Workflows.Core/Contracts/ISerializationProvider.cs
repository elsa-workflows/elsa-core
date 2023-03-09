using System.Text.Json;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Provides custom serialization for a given value.
/// Use this when you need to serialize/deserialize a value that is not supported by the default serializer, such as file streams that you want to serialize as a URL.
/// </summary>
public interface ISerializationProvider
{
    /// <summary>
    /// The priority of this serialization provider. The higher the priority, the more likely it is to be used.
    /// </summary>
    public float Priority { get; set; }
    
    /// <summary>
    /// True if this serialization provider supports the specified value.
    /// </summary>
    public bool Supports(object? value);
    
    /// <summary>
    /// Serializes the specified value.
    /// </summary>
    ValueTask<JsonElement> SerializeAsync(object? value, CancellationToken cancellationToken = default);
}