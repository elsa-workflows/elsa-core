using Elsa.Common.Models;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Service wrapper for cancelling multiple workflow instances.
/// </summary>
public interface IWorkflowCancellationService
{
    /// <summary>
    /// Cancels a workflow instance.
    /// </summary>
    /// <remarks>Also cancels all children</remarks>
    Task<bool> CancelWorkflowAsync(string workflowInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels workflow executions with the specified workflow instance ID.
    /// </summary>
    /// <remarks>Also cancels all children</remarks>
    Task<int> CancelWorkflowsAsync(IEnumerable<string> workflowInstanceIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels all workflow instances by definition version ID.
    /// </summary>
    /// <remarks>Also cancels all children</remarks>
    Task<int> CancelWorkflowByDefinitionVersionAsync(string definitionVersionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels all workflows that match the specified workflow definition by its ID and version.
    /// </summary>
    /// <remarks>Also cancels all children</remarks>
    Task<int> CancelWorkflowByDefinitionAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);

}