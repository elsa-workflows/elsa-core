using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Workflows.Sinks.Contracts;
using Elsa.Workflows.Sinks.Models;
using Elsa.Workflows.Core.Serialization;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.WorkflowSink;

public class EFCoreWorkflowSinkClient : IWorkflowSinkClient
{
    private readonly Store<WorkflowSinkElsaDbContext, WorkflowInstance> _store;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;

    public EFCoreWorkflowSinkClient(Store<WorkflowSinkElsaDbContext, WorkflowInstance> store, SerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    public async Task SaveAsync(WorkflowInstanceDto dto, CancellationToken cancellationToken = default)
    {
        var existingEntity = await _store.FindAsync(e => e.Id == dto.Id, cancellationToken);

        if (existingEntity?.LastExecutedAt == dto.LastExecutedAt) return;
        
        existingEntity ??= new WorkflowInstance
        {
            Id = dto.WorkflowState.Id,
            CreatedAt = dto.CreatedAt
        };

        existingEntity.LastExecutedAt = dto.LastExecutedAt;
        existingEntity.CancelledAt = dto.CancelledAt;
        existingEntity.FinishedAt = dto.FinishedAt;
        existingEntity.FaultedAt = dto.FaultedAt;

        await _store.SaveAsync(existingEntity, dto, OnSaving, cancellationToken);
    }

    private WorkflowInstance OnSaving(WorkflowSinkElsaDbContext wfSinkElsaDbContext, WorkflowInstance entity, WorkflowInstanceDto dto)
    {
        var data = new { dto.Workflow, dto.WorkflowState};

        var options = _serializerOptionsProvider.CreatePersistenceOptions(ReferenceHandler.Preserve);
        var json = JsonSerializer.Serialize(data, options);

        wfSinkElsaDbContext.Entry(entity).Property("Data").CurrentValue = json;
        return entity;
    }
}