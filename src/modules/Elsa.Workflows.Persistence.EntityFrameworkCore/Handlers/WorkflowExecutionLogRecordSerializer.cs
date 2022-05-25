using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Persistence.EntityFrameworkCore.Common.Services;
using Elsa.Workflows.Persistence.Entities;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Handlers;

public class WorkflowExecutionLogRecordSerializer : IEntitySerializer<WorkflowsDbContext, WorkflowExecutionLogRecord>
{
    private readonly JsonSerializerOptions _options;

    public WorkflowExecutionLogRecordSerializer()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new JsonStringEnumConverter());
    }

    public void Serialize(WorkflowsDbContext dbContext, WorkflowExecutionLogRecord entity)
    {
        var json = entity.Payload != null ? JsonSerializer.Serialize(entity.Payload, _options) : default!;
        dbContext.Entry(entity).Property("PayloadData").CurrentValue = json;
    }

    public void Deserialize(WorkflowsDbContext dbContext, WorkflowExecutionLogRecord entity)
    {
        var json = (string?)dbContext.Entry(entity).Property("PayloadData").CurrentValue;

        if (!string.IsNullOrWhiteSpace(json)) entity.Payload = JsonSerializer.Deserialize<object>(json, _options)!;
    }
}