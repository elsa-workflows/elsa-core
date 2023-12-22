using Elsa.Workflows.Core.Activities;

namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// Serializes and deserializes workflows.
/// </summary>
public interface IWorkflowSerializer
{
    /// <summary>
    /// Serializes the specified workflow.
    /// </summary>
    /// <param name="serializedWorkflow">The data representing the workflow.</param>
    /// <returns>A deserialized workflow.</returns>
    Workflow Deserialize(string serializedWorkflow);
}