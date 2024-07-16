using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Populates the <see cref="IWorkflowDefinitionStore"/> with workflow definitions provided from <see cref="IWorkflowProvider"/> implementations.
/// </summary>
public interface IWorkflowDefinitionStorePopulator
{
    /// <summary>
    /// Populates the <see cref="IWorkflowDefinitionStore"/> with workflow definitions provided from <see cref="IWorkflowProvider"/> implementations.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IEnumerable<WorkflowDefinition>> PopulateStoreAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Populates the <see cref="IWorkflowDefinitionStore"/> with workflow definitions provided from <see cref="IWorkflowProvider"/> implementations.
    /// </summary>
    /// <param name="indexTriggers">Whether to index triggers.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IEnumerable<WorkflowDefinition>> PopulateStoreAsync(bool indexTriggers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a workflow definition to the store.
    /// </summary>
    /// <param name="materializedWorkflow">A materialized workflow.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<WorkflowDefinition> AddAsync(MaterializedWorkflow materializedWorkflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a workflow definition to the store.
    /// </summary>
    /// <param name="materializedWorkflow">A materialized workflow.</param>
    /// <param name="indexTriggers">Whether to index triggers.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<WorkflowDefinition> AddAsync(MaterializedWorkflow materializedWorkflow, bool indexTriggers, CancellationToken cancellationToken = default);
}