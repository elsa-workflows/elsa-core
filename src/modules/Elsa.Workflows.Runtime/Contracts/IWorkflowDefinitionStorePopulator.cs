using Elsa.Workflows.Management.Contracts;

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
    Task PopulateStoreAsync(CancellationToken cancellationToken = default);
}