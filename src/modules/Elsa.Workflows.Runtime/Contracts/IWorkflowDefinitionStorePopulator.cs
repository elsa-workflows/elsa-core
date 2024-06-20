using Elsa.Workflows.Management;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Populates the <see cref="IWorkflowDefinitionStore"/> with workflow definitions provided from <see cref="IWorkflowsProvider"/> implementations.
/// </summary>
public interface IWorkflowDefinitionStorePopulator
{
    /// <summary>
    /// Populates the <see cref="IWorkflowDefinitionStore"/> with workflow definitions provided from <see cref="IWorkflowsProvider"/> implementations.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PopulateStoreAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Populates the <see cref="IWorkflowDefinitionStore"/> with workflow definitions provided from <see cref="IWorkflowProvider"/> implementations.
    /// </summary>
    /// <param name="indexTriggers">Whether to index triggers.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PopulateStoreAsync(bool indexTriggers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a workflow definition to the store.
    /// </summary>
    /// <param name="materializedWorkflow">A materialized workflow.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task AddAsync(MaterializedWorkflow materializedWorkflow, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a workflow definition to the store.
    /// </summary>
    /// <param name="materializedWorkflow">A materialized workflow.</param>
    /// /// <param name="indexTriggers">Whether to index triggers.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task AddAsync(MaterializedWorkflow materializedWorkflow, bool indexTriggers, CancellationToken cancellationToken = default);
}