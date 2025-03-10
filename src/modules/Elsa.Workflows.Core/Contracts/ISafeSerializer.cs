using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Elsa.Workflows;

/// <summary>
/// Serializes and deserializes activity state. Only primitive and serializable values are supported.
/// </summary>
public interface ISafeSerializer
{ 
    /// <summary>
    /// Serializes the specified state.
    /// </summary>
    [Obsolete("Use the non-async Serialize instead.")]
    [RequiresUnreferencedCode("The type T may be trimmed.")]
    ValueTask<string> SerializeAsync(object? value, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Serializes the specified state to a <see cref="JsonElement"/> object.
    /// </summary>
    [Obsolete("Use the non-async SerializeToElement instead.")]
    [RequiresUnreferencedCode("The type T may be trimmed.")]
    ValueTask<JsonElement> SerializeToElementAsync(object? value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deserializes the specified state.
    /// </summary>
    [Obsolete("Use the non-async Deserialize instead.")]
    [RequiresUnreferencedCode("The type T may be trimmed.")]
    ValueTask<T> DeserializeAsync<T>(string json, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deserializes the specified state.
    /// </summary>
    [Obsolete("Use the non-async Deserialize instead.")]
    [RequiresUnreferencedCode("The type T may be trimmed.")]
    ValueTask<T> DeserializeAsync<T>(JsonElement element, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Serializes the specified state.
    /// </summary>
    [RequiresUnreferencedCode("The type T may be trimmed.")]
    string Serialize(object? value);
    
    /// <summary>
    /// Serializes the specified state to a <see cref="JsonElement"/> object.
    /// </summary>
    [RequiresUnreferencedCode("The type T may be trimmed.")]
    JsonElement SerializeToElement(object? value);

    /// <summary>
    /// Deserializes the specified state.
    /// </summary>
    [RequiresUnreferencedCode("The type T may be trimmed.")]
    T Deserialize<T>(string json);
    
    /// <summary>
    /// Deserializes the specified state.
    /// </summary>
    [RequiresUnreferencedCode("The type T may be trimmed.")]
    T Deserialize<T>(JsonElement element);
    
    /// <summary>
    /// Gets the JSON serializer options.
    /// </summary>
    JsonSerializerOptions GetOptions();
}