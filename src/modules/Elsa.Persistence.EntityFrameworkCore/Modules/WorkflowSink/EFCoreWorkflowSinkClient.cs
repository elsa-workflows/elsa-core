using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Workflows.Sink.Contracts;
using Elsa.Workflows.Sink.Models;
using Elsa.Workflows.Core.Serialization;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.WorkflowSink;

public class EFCoreWorkflowSinkClient : IWorkflowSinkClient
{
    private readonly Store<WorkflowSinkElsaDbContext, WorkflowSinkEntity> _store;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;

    public EFCoreWorkflowSinkClient(Store<WorkflowSinkElsaDbContext, WorkflowSinkEntity> store, SerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    public async Task SaveAsync(WorkflowSinkDto dto, CancellationToken cancellationToken = default)
    {
        var existingEntity = await _store.FindAsync(e => e.Id == dto.Id, cancellationToken);

        if (existingEntity?.LastExecutedAt == dto.LastExecutedAt) return;
        
        existingEntity ??= new WorkflowSinkEntity
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

    private WorkflowSinkEntity OnSaving(WorkflowSinkElsaDbContext wfSinkElsaDbContext, WorkflowSinkEntity entity, WorkflowSinkDto dto)
    {
        var data = new { dto.Workflow, dto.WorkflowState};

        var options = _serializerOptionsProvider.CreatePersistenceOptions(ReferenceHandler.Preserve);
        var json = JsonSerializer.Serialize(data, options);

        wfSinkElsaDbContext.Entry(entity).Property("Data").CurrentValue = json;
        return entity;
    }
}