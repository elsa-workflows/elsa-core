namespace Elsa.Workflows.Management;

/// <summary>
/// Finds all latest versions of workflow definitions that reference a specific workflow definition.
/// </summary>
public interface IWorkflowReferenceQuery
{
    /// <summary>
    /// Queries all latest versions of workflow definitions that reference the specified workflow definition.
    /// </summary>
    /// <param name="workflowDefinitionId">The ID of the workflow definition to query references for.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A collection of workflow definition IDs that reference the specified workflow definition.</returns>
    Task<IEnumerable<string>> ExecuteAsync(string workflowDefinitionId, CancellationToken cancellationToken = default);
}