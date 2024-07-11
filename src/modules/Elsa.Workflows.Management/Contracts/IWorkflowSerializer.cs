using Elsa.Workflows.Activities;

namespace Elsa.Workflows.Management;

/// <summary>
/// Serializes and deserializes workflows.
/// </summary>
public interface IWorkflowSerializer
{
    /// <summary>
    /// Serializes the specified workflow.
    /// </summary>
    /// <param name="workflow">The workflow.</param>
    /// <returns>A string representing the serialized workflow.</returns>
    string Serialize(Workflow workflow);
    
    /// <summary>
    /// Serializes the specified workflow.
    /// </summary>
    /// <param name="serializedWorkflow">The data representing the workflow.</param>
    /// <returns>A deserialized workflow.</returns>
    Workflow Deserialize(string serializedWorkflow);
}