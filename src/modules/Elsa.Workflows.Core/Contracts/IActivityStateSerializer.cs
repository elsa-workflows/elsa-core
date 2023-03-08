using System.Text.Json;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Serializes and deserializes activity state. Only primitive and serializable values are supported.
/// To provide custom serialization, implement <see cref="ISerializationProvider"/> and register it with the service container.
/// </summary>
public interface IActivityStateSerializer
{ 
    /// <summary>
    /// Serializes the specified state.
    /// </summary>
    Task<JsonElement> SerializeAsync(object? value, CancellationToken cancellationToken = default);
}