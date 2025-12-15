using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Api;

/// <summary>
/// Defines a contract for generating a filename suitable for exporting a workflow instance.
/// </summary>
public interface IWorkflowInstanceExportNameProvider
{
    /// <summary>
    /// Generates a filename for a workflow instance export.
    /// </summary>
    /// <param name="instance">The workflow instance being exported.</param>
    /// <param name="model">The export model representing the instance (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A filename safe for saving to disk.</returns>
    Task<string> GetFileNameAsync(WorkflowInstance instance, object model, CancellationToken cancellationToken = default);
}
