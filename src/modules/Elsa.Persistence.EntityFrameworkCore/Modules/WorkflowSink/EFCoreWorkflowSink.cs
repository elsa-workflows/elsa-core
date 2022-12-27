using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Workflows.Sink.Contracts;
using Elsa.Workflows.Sink.Models;
using Elsa.Workflows.Core.Serialization;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.WorkflowSink;

public class EFCoreWorkflowSink : IEFCoreWorkflowSink
{
    private readonly Store<WorkflowSinkElsaDbContext, Workflows.Sink.Models.WorkflowSink> _store;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;

    public EFCoreWorkflowSink(Store<WorkflowSinkElsaDbContext, Workflows.Sink.Models.WorkflowSink> store, SerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    public async Task SaveAsync(WorkflowSinkDto dto, CancellationToken cancellationToken = default)
    {
        var workflowSinkEntity = new Workflows.Sink.Models.WorkflowSink
        {
            Id = dto.Id,
            CreatedAt = dto.CreatedAt,
            LastExecutedAt = dto.LastExecutedAt,
            CancelledAt = dto.CancelledAt,
            FinishedAt = dto.FinishedAt,
            FaultedAt = dto.FaultedAt
        };
        await AdjustWithExistingEntity(dto, cancellationToken);
        await _store.SaveAsync(workflowSinkEntity, dto, OnSaving, cancellationToken);
    }

    private async Task AdjustWithExistingEntity(WorkflowSinkDto record, CancellationToken cancellationToken = default)
    {
        var existingEntity = await _store.FindAsync(e => e.Id == record.Id, cancellationToken);
        record.CreatedAt = existingEntity?.CreatedAt ?? record.CreatedAt;
    }
    
    private Workflows.Sink.Models.WorkflowSink OnSaving(WorkflowSinkElsaDbContext wfSinkElsaDbContext, Workflows.Sink.Models.WorkflowSink entity, WorkflowSinkDto dto)
    {
        var data = new { dto.Workflow, dto.WorkflowState};

        var options = _serializerOptionsProvider.CreatePersistenceOptions(ReferenceHandler.Preserve);
        var json = JsonSerializer.Serialize(data, options);

        wfSinkElsaDbContext.Entry(entity).Property("Data").CurrentValue = json;
        return entity;
    }
}