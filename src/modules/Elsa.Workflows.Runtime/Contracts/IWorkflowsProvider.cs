namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents a source of workflow definitions.
/// </summary>
public interface IWorkflowsProvider
{
    /// <summary>
    /// Gets the name of the provider.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets the workflow definitions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The workflow definitions.</returns>
    ValueTask<IEnumerable<MaterializedWorkflow>> GetWorkflowsAsync(CancellationToken cancellationToken = default);
}