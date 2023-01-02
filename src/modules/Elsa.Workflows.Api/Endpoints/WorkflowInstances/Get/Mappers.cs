using Elsa.Workflows.Management.Entities;
using FastEndpoints;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Get;

/// <summary>
/// Maps a <see cref="WorkflowInstance"/> to <see cref="Response"/>.
/// </summary>
public class WorkflowInstanceMapper : ResponseMapper<Response, WorkflowInstance>
{
    /// <inheritdoc />
    public override Response FromEntity(WorkflowInstance e) => new()
    {
        Id = e.Id,
        DefinitionId = e.DefinitionId,
        DefinitionVersionId = e.DefinitionVersionId,
        Version = e.Version,
        WorkflowState = e.WorkflowState,
        Status = e.Status,
        SubStatus = e.SubStatus,
        CorrelationId = e.CorrelationId,
        Name = e.Name,
        Fault = e.Fault,
        CancelledAt = e.CancelledAt,
        CreatedAt = e.CreatedAt,
        FaultedAt = e.FaultedAt,
        FinishedAt = e.FinishedAt,
        LastExecutedAt = e.LastExecutedAt
    };
}