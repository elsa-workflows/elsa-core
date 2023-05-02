using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Entities;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Models;

/// <summary>
/// Represents a summary view of a <see cref="WorkflowInstance"/>.
/// </summary>
/// <param name="Id">The ID of the workflow instance.</param>
/// <param name="DefinitionId">The ID of the workflow definition.</param>
/// <param name="DefinitionVersionId">The version ID of the workflow definition.</param>
/// <param name="Version">The version of the workflow definition.</param>
/// <param name="Status">The status of the workflow instance.</param>
/// <param name="SubStatus">The sub-status of the workflow instance.</param>
/// <param name="CorrelationId">The ID of the workflow instance.</param>
/// <param name="Name">The name of the workflow instance.</param>
/// <param name="CreatedAt">The timestamp when the workflow instance was created.</param>
/// <param name="LastExecutedAt">The timestamp when the workflow instance was last executed.</param>
/// <param name="FinishedAt">The timestamp when the workflow instance was finished.</param>
/// <param name="CancelledAt">The timestamp when the workflow instance was cancelled.</param>
/// <param name="FaultedAt">The timestamp when the workflow instance was faulted.</param>
[PublicAPI]
public record WorkflowInstanceSummary(
    string Id,
    string DefinitionId,
    string DefinitionVersionId,
    int Version,
    WorkflowStatus Status,
    WorkflowSubStatus SubStatus,
    string? CorrelationId,
    string? Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastExecutedAt,
    DateTimeOffset? FinishedAt,
    DateTimeOffset? CancelledAt,
    DateTimeOffset? FaultedAt)
{
    /// <summary>
    /// Returns a summary view of the specified <see cref="WorkflowInstance"/>.
    /// </summary>
    public static WorkflowInstanceSummary FromInstance(WorkflowInstance workflowInstance)
    {
        return new(
            workflowInstance.Id,
            workflowInstance.DefinitionId,
            workflowInstance.DefinitionVersionId,
            workflowInstance.Version,
            workflowInstance.Status,
            workflowInstance.SubStatus,
            workflowInstance.CorrelationId,
            workflowInstance.Name,
            workflowInstance.CreatedAt,
            workflowInstance.LastExecutedAt,
            workflowInstance.FinishedAt,
            workflowInstance.CancelledAt,
            workflowInstance.FaultedAt
        );
    }
}