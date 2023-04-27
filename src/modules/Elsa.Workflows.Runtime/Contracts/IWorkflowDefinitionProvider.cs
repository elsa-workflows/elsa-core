using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Represents a source of workflow definitions.
/// </summary>
public interface IWorkflowDefinitionProvider
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
    ValueTask<IEnumerable<WorkflowDefinitionResult>> GetWorkflowDefinitionsAsync(CancellationToken cancellationToken = default);
}