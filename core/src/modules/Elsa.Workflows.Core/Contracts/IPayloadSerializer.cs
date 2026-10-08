using System.Text.Json;

namespace Elsa.Workflows;

/// <summary>
/// Serializes execution log record payloads.
/// </summary>
public interface IPayloadSerializer
{
    /// <summary>
    /// Serializes the payload.
    /// </summary>
    /// <param name="payload">the payload to serialize.</param>
    /// <returns>The serialized payload.</returns>
    string Serialize(object payload);

    /// <summary>
    /// Serializes the payload.
    /// </summary>
    /// <param name="payload">the payload to serialize.</param>
    /// <returns>The serialized payload.</returns>
    JsonElement SerializeToElement(object payload);

    /// <summary>
    /// Deserializes the specified serialized state.
    /// </summary>
    /// <param name="serializedData">The serialized state.</param>
    /// <returns>The deserialized state.</returns>
    object Deserialize(string serializedData);

    /// <summary>
    /// Deserializes the specified serialized state.
    /// </summary>
    /// <param name="serializedData">The serialized state.</param>
    /// <param name="type">The type to deserialize the state into.</param>
    /// <returns>The deserialized state.</returns>
    object Deserialize(string serializedData, Type type);

    /// <summary>
    /// Deserializes the specified serialized state.
    /// </summary>
    /// <param name="serializedData">The serialized state.</param>
    /// <returns>The deserialized state.</returns>
    object Deserialize(JsonElement serializedData);

    /// <summary>
    /// Deserializes the specified serialized state.
    /// </summary>
    /// <param name="serializedData">The serialized state.</param>
    /// <returns>The deserialized state.</returns>
    T Deserialize<T>(string serializedData);

    /// <summary>
    /// Deserializes the specified serialized state.
    /// </summary>
    /// <param name="serializedData">The serialized state.</param>
    /// <returns>The deserialized state.</returns>
    T Deserialize<T>(JsonElement serializedData);
    
    /// <summary>
    /// Gets the JSON serializer options.
    /// </summary>
    JsonSerializerOptions GetOptions();
}