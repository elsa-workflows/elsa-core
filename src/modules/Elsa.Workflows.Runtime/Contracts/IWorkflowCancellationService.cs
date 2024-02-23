using Elsa.Common.Models;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Service wrapper for cancelling multiple workflow instances.
/// </summary>
public interface IWorkflowCancellationService
{
    /// <summary>
    /// Cancels workflow executions with the specified workflow instance ID.
    /// </summary>
    Task<int> CancelWorkflowsAsync(IEnumerable<string> workflowInstanceIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels all workflow instances by definition version ID.
    /// </summary>
    Task<int> CancelWorkflowByDefinitionVersionAsync(string definitionVersionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels all workflows that match the specified workflow definition by its ID and version.
    /// </summary>
    Task<int> CancelWorkflowByDefinitionAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);
}