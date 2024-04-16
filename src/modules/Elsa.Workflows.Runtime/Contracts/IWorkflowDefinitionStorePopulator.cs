using Elsa.Workflows.Management.Contracts;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Populates the <see cref="IWorkflowDefinitionStore"/> with workflow definitions provided from <see cref="IWorkflowProvider"/> implementations.
/// </summary>
public interface IWorkflowDefinitionStorePopulator
{
    /// <summary>
    /// Populates the <see cref="IWorkflowDefinitionStore"/> with workflow definitions provided from <see cref="IWorkflowProvider"/> implementations.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PopulateStoreAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a workflow definition to the store.
    /// </summary>
    /// <param name="materializedWorkflow">A materialized workflow.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task AddAsync(MaterializedWorkflow materializedWorkflow, CancellationToken cancellationToken = default);
}