using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Sinks.Contracts;
using Elsa.Workflows.Sinks.Models;
using Elsa.Workflows.Core.Serialization;

namespace Elsa.EntityFrameworkCore.Modules.WorkflowSink;

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
        
        var dataToSerialize = new { dto.Workflow, dto.WorkflowState};
        var options = _serializerOptionsProvider.CreatePersistenceOptions(ReferenceHandler.Preserve);
        var json = JsonSerializer.Serialize(dataToSerialize, options);
        (await _store.GetDbContextAsync(cancellationToken)).Entry(existingEntity).Property("Data").CurrentValue = json;

        await _store.SaveAsync(existingEntity, cancellationToken);
    }
}