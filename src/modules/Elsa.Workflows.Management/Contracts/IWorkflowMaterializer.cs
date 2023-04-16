using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// A service that can materialize a workflow from a workflow definition.
/// </summary>
public interface IWorkflowMaterializer
{
    /// <summary>
    /// The name of the materializer.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Materializes a workflow from a workflow definition.
    /// </summary>
    /// <param name="definition">The workflow definition.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The materialized workflow.</returns>
    ValueTask<Workflow> MaterializeAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
}