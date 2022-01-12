using System.Text.Json;
using Elsa.Management.Serialization;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Contracts;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Serialization;

public class WorkflowExecutionLogRecordSerializer : IEntitySerializer<WorkflowExecutionLogRecord>
{
    private readonly WorkflowSerializerOptionsProvider _workflowSerializerOptionsProvider;

    public WorkflowExecutionLogRecordSerializer(WorkflowSerializerOptionsProvider workflowSerializerOptionsProvider)
    {
        _workflowSerializerOptionsProvider = workflowSerializerOptionsProvider;
    }

    public void Serialize(ElsaDbContext dbContext, WorkflowExecutionLogRecord entity)
    {
        var options = _workflowSerializerOptionsProvider.CreatePersistenceOptions();
        var json = entity.Payload != null ? JsonSerializer.Serialize(entity.Payload, options) : default!;

        dbContext.Entry(entity).Property("PayloadData").CurrentValue = json;
    }

    public void Deserialize(ElsaDbContext dbContext, WorkflowExecutionLogRecord entity)
    {
        var json = (string?)dbContext.Entry(entity).Property("PayloadData").CurrentValue;

        if (!string.IsNullOrWhiteSpace(json))
        {
            var options = _workflowSerializerOptionsProvider.CreatePersistenceOptions();
            entity.Payload = JsonSerializer.Deserialize<object>(json, options)!;
        }
    }
}