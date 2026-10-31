namespace Elsa.Studio.Workflows.UI.Contracts;

/// <summary>
/// Represents a workspace.
/// </summary>
public interface IWorkspace
{
    /// <summary>
    /// Gets a value indicating whether the workspace is read-only.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Geta a value indicating whether the user has permissions to edit the workflow.
    /// </summary>
    bool HasWorkflowEditPermission { get; }
}