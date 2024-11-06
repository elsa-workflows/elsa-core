using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// Defines a visitor that can traverse and process workflow definitions.
/// </summary>
public interface IWorkflowGraphNetworkBuilder
{
    /// <summary>
    /// Builds a network of workflow graphs and their consumers.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A network of workflow graphs and their consumers.</returns>
    Task<WorkflowGraphNetwork> BuildAsync(CancellationToken cancellationToken = default);
}