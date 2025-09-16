using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Management.Entities;
using FastEndpoints;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Get;

/// <summary>
/// Maps a <see cref="WorkflowInstance"/> to <see cref="WorkflowInstanceModel"/>.
/// </summary>
internal class WorkflowInstanceMapper : ResponseMapper<WorkflowInstanceModel, WorkflowInstance>, IRequestMapper
{
    /// <inheritdoc />
    public override WorkflowInstanceModel FromEntity(WorkflowInstance e) => new()
    {
        Id = e.Id,
        DefinitionId = e.DefinitionId,
        DefinitionVersionId = e.DefinitionVersionId,
        Version = e.Version,
        ParentWorkflowInstanceId = e.ParentWorkflowInstanceId,
        WorkflowState = e.WorkflowState,
        Status = e.Status,
        SubStatus = e.SubStatus,
        IsExecuting = e.IsExecuting,
        CorrelationId = e.CorrelationId,
        Name = e.Name,
        IncidentCount = e.IncidentCount,
        IsSystem = e.IsSystem,
        CreatedAt = e.CreatedAt,
        FinishedAt = e.FinishedAt,
        UpdatedAt = e.UpdatedAt,
        Initiator = e.Initiator
    };
}