namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Serializes and execution log record payloads.
/// </summary>
public interface IPayloadSerializer
{
    /// <summary>
    /// Serializes the specified workflow state.
    /// </summary>
    /// <param name="payload">state to serialize.</param>
    /// <returns>The serialized state.</returns>
    string Serialize(object payload);
    
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
    /// <returns>The deserialized state.</returns>
    T Deserialize<T>(string serializedData);
}