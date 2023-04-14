using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Contracts;

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
    Task<string> SerializeAsync(WorkflowState workflowState, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deserializes the specified serialized state.
    /// </summary>
    /// <param name="serializedState">The serialized state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized workflow state.</returns>
    Task<WorkflowState> DeserializeAsync(string serializedState, CancellationToken cancellationToken = default);
}