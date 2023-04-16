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
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The serialized state.</returns>
    Task<string> SerializeAsync(object payload, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deserializes the specified serialized state.
    /// </summary>
    /// <param name="serializedData">The serialized state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized state.</returns>
    Task<object> DeserializeAsync(string serializedData, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deserializes the specified serialized state.
    /// </summary>
    /// <param name="serializedData">The serialized state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized state.</returns>
    Task<T> DeserializeAsync<T>(string serializedData, CancellationToken cancellationToken = default);
}