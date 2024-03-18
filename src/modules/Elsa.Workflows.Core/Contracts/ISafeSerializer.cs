using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Elsa.Workflows.Contracts;

/// <summary>
/// Serializes and deserializes activity state. Only primitive and serializable values are supported.
/// </summary>
public interface ISafeSerializer
{ 
    /// <summary>
    /// Serializes the specified state.
    /// </summary>
    [RequiresUnreferencedCode("The type T may be trimmed.")]
    ValueTask<string> SerializeAsync(object? value, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Serializes the specified state to a <see cref="JsonElement"/> object.
    /// </summary>
    [RequiresUnreferencedCode("The type T may be trimmed.")]
    ValueTask<JsonElement> SerializeToElementAsync(object? value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deserializes the specified state.
    /// </summary>
    [RequiresUnreferencedCode("The type T may be trimmed.")]
    ValueTask<T> DeserializeAsync<T>(string json, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deserializes the specified state.
    /// </summary>
    [RequiresUnreferencedCode("The type T may be trimmed.")]
    ValueTask<T> DeserializeAsync<T>(JsonElement element, CancellationToken cancellationToken = default);
}