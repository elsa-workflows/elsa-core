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
    string Serialize(object? value);

    /// <summary>
    /// Deserializes the specified state.
    /// </summary>
    T Deserialize<T>(string json);
}