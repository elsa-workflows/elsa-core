using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Elsa.Workflows.State;

namespace Elsa.Workflows;

/// <summary>
/// Serializes and deserializes workflow states.
/// </summary>
public interface IWorkflowStateSerializer
{
    /// <summary>
    /// Serializes the specified workflow state.
    /// </summary>
    /// <param name="workflowState">The workflow state to serialize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The serialized workflow state.</returns>
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The serialization process may require access to the type.")]
    [Obsolete("Use the non-async version Serialize instead.")]
    Task<string> SerializeAsync(WorkflowState workflowState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes the specified workflow state.
    /// </summary>
    /// <param name="workflowState">The workflow state to serialize.</param>
    /// <returns>The serialized workflow state.</returns>
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The serialization process may require access to the type.")]
    string Serialize(WorkflowState workflowState);

    /// <summary>
    /// Serializes the specified workflow state.
    /// </summary>
    /// <param name="workflowState">The workflow state to serialize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The serialized workflow state.</returns>
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The serialization process may require access to the type.")]
    [Obsolete("Use the non-async version SerializeToUtfBytes instead.")]
    Task<byte[]> SerializeToUtfBytesAsync(WorkflowState workflowState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes the specified workflow state.
    /// </summary>
    /// <param name="workflowState">The workflow state to serialize.</param>
    /// <returns>The serialized workflow state.</returns>
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The serialization process may require access to the type.")]
    byte[] SerializeToUtfBytes(WorkflowState workflowState);

    /// <summary>
    /// Serializes the specified workflow state.
    /// </summary>
    /// <param name="workflowState">The workflow state to serialize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The serialized workflow state.</returns>
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The serialization process may require access to the type.")]
    [Obsolete("Use the non-async version SerializeToElement instead.")]
    Task<JsonElement> SerializeToElementAsync(WorkflowState workflowState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes the specified workflow state.
    /// </summary>
    /// <param name="workflowState">The workflow state to serialize.</param>
    /// <returns>The serialized workflow state.</returns>
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The serialization process may require access to the type.")]
    JsonElement SerializeToElement(WorkflowState workflowState);

    /// <summary>
    /// Serializes the specified workflow state.
    /// </summary>
    /// <param name="workflowState">The workflow state to serialize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The serialized workflow state.</returns>
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The serialization process may require access to the type.")]
    [Obsolete("Use the non-async version Serialize instead.")]
    Task<string> SerializeAsync(object workflowState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes the specified workflow state.
    /// </summary>
    /// <param name="workflowState">The workflow state to serialize.</param>
    /// <returns>The serialized workflow state.</returns>
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The serialization process may require access to the type.")]
    string Serialize(object workflowState);

    /// <summary>
    /// Deserializes the specified serialized state.
    /// </summary>
    /// <param name="serializedState">The serialized state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized workflow state.</returns>
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The deserialization process may require access to the type.")]
    [Obsolete("Use the non-async version Deserialize instead.")]
    Task<WorkflowState> DeserializeAsync(string serializedState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deserializes the specified serialized state.
    /// </summary>
    /// <param name="serializedState">The serialized state.</param>
    /// <returns>The deserialized workflow state.</returns>
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The deserialization process may require access to the type.")]
    WorkflowState Deserialize(string serializedState);

    /// <summary>
    /// Deserializes the specified serialized state.
    /// </summary>
    /// <param name="serializedState">The serialized state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized workflow state.</returns>
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The deserialization process may require access to the type.")]
    [Obsolete("Use the non-async version Deserialize instead.")]
    Task<WorkflowState> DeserializeAsync(JsonElement serializedState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deserializes the specified serialized state.
    /// </summary>
    /// <param name="serializedState">The serialized state.</param>
    /// <returns>The deserialized workflow state.</returns>
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The deserialization process may require access to the type.")]
    WorkflowState Deserialize(JsonElement serializedState);

    /// <summary>
    /// Deserializes the specified serialized state.
    /// </summary>
    /// <param name="serializedState">The serialized state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized workflow state.</returns>
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The deserialization process may require access to the type.")]
    [Obsolete("Use the non-async version Deserialize instead.")]
    Task<T> DeserializeAsync<T>(string serializedState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deserializes the specified serialized state.
    /// </summary>
    /// <param name="serializedState">The serialized state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized workflow state.</returns>
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The deserialization process may require access to the type.")]
    T Deserialize<T>(string serializedState);
}