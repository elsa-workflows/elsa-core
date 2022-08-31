using Elsa.Workflows.Persistence.Entities;
using FastEndpoints;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Get;

public class WorkflowInstanceMapper : ResponseMapper<Response, WorkflowInstance>
{
    public override Response FromEntity(WorkflowInstance e) => new()
    {
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