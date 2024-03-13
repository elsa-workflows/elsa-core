using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Contracts;

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
    Task<string> SerializeAsync(WorkflowState workflowState, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Serializes the specified workflow state.
    /// </summary>
    /// <param name="workflowState">The workflow state to serialize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The serialized workflow state.</returns>
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The serialization process may require access to the type.")]
    Task<byte[]> SerializeToUtfBytesAsync(WorkflowState workflowState, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Serializes the specified workflow state.
    /// </summary>
    /// <param name="workflowState">The workflow state to serialize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The serialized workflow state.</returns>
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The serialization process may require access to the type.")]
    Task<JsonElement> SerializeToElementAsync(WorkflowState workflowState, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Serializes the specified workflow state.
    /// </summary>
    /// <param name="workflowState">The workflow state to serialize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The serialized workflow state.</returns>
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The serialization process may require access to the type.")]
    Task<string> SerializeAsync(object workflowState, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deserializes the specified serialized state.
    /// </summary>
    /// <param name="serializedState">The serialized state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized workflow state.</returns>
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The serialization process may require access to the type.")]
    Task<WorkflowState> DeserializeAsync(string serializedState, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deserializes the specified serialized state.
    /// </summary>
    /// <param name="serializedState">The serialized state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized workflow state.</returns>
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The serialization process may require access to the type.")]
    Task<WorkflowState> DeserializeAsync(JsonElement serializedState, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deserializes the specified serialized state.
    /// </summary>
    /// <param name="serializedState">The serialized state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized workflow state.</returns>
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The serialization process may require access to the type.")]
    Task<T> DeserializeAsync<T>(string serializedState, CancellationToken cancellationToken = default);
}