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
    /// <returns>The serialized workflow state.</returns>
    string Serialize(WorkflowState workflowState);

    /// <summary>
    /// Serializes the specified workflow state.
    /// </summary>
    /// <param name="workflowState">The workflow state to serialize.</param>
    /// <returns>The serialized workflow state.</returns>
    byte[] SerializeToUtfBytes(WorkflowState workflowState);

    /// <summary>
    /// Serializes the specified workflow state.
    /// </summary>
    /// <param name="workflowState">The workflow state to serialize.</param>
    /// <returns>The serialized workflow state.</returns>
    JsonElement SerializeToElement(WorkflowState workflowState);

    /// <summary>
    /// Serializes the specified workflow state.
    /// </summary>
    /// <param name="workflowState">The workflow state to serialize.</param>
    /// <returns>The serialized workflow state.</returns>
    string Serialize(object workflowState);

    /// <summary>
    /// Deserializes the specified serialized state.
    /// </summary>
    /// <param name="serializedState">The serialized state.</param>
    /// <returns>The deserialized workflow state.</returns>
    WorkflowState Deserialize(string serializedState);

    /// <summary>
    /// Deserializes the specified serialized state.
    /// </summary>
    /// <param name="serializedState">The serialized state.</param>
    /// <returns>The deserialized workflow state.</returns>
    WorkflowState Deserialize(JsonElement serializedState);

    /// <summary>
    /// Deserializes the specified serialized state.
    /// </summary>
    /// <param name="serializedState">The serialized state.</param>
    /// <returns>The deserialized workflow state.</returns>
    T Deserialize<T>(string serializedState);
}