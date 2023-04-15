namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Serializes and execution log record payloads.
/// </summary>
public interface IWorkflowExecutionLogStateSerializer
{
    /// <summary>
    /// Serializes the specified workflow state.
    /// </summary>
    /// <param name="payload">state to serialize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The serialized state.</returns>
    Task<string> SerializeAsync(object payload, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Serializes the specified workflow state.
    /// </summary>
    /// <param name="payload">state to serialize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The serialized state.</returns>
    Task<string> SerializeAsync(IDictionary<string, object> payload, CancellationToken cancellationToken = default);

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
    Task<IDictionary<string, object>> DeserializeDictionaryAsync(string serializedData, CancellationToken cancellationToken = default);
}