using Elsa.Workflows.Activities;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Registers workflows.
/// </summary>
public interface IWorkflowRegistry
{
    /// <summary>
    /// Registers a workflow.
    /// </summary>
    /// <param name="workflow">The workflow to register.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task RegisterAsync(Workflow workflow, CancellationToken cancellationToken = default);
}