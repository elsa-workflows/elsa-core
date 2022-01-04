using System.Text.Json;
using Elsa.Management.Serialization;
using Elsa.Management.Serialization.Extensions;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Contracts;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Serialization;

public class WorkflowInstanceSerializer : IEntitySerializer<WorkflowInstance>
{
    private readonly WorkflowSerializerOptionsProvider _workflowSerializerOptionsProvider;

    public WorkflowInstanceSerializer(WorkflowSerializerOptionsProvider workflowSerializerOptionsProvider)
    {
        _workflowSerializerOptionsProvider = workflowSerializerOptionsProvider;
    }
    
    public void Serialize(ElsaDbContext dbContext, WorkflowInstance entity)
    {
        var data = new
        {
            entity.WorkflowState
        };

        var options = _workflowSerializerOptionsProvider.CreateSerializerOptions();
        var json = JsonSerializer.Serialize(data, options);

        dbContext.Entry(entity).Property("Data").CurrentValue = json;
    }

    public void Deserialize(ElsaDbContext dbContext, WorkflowInstance entity)
    {
        var data = new
        {
            entity.WorkflowState
        };

        var json = (string?) dbContext.Entry(entity).Property("Data").CurrentValue;

        if (!string.IsNullOrWhiteSpace(json))
        {
            var options = _workflowSerializerOptionsProvider.CreateSerializerOptions();
            data = JsonSerializerExtensions.DeserializeAnonymousType(json, data, options)!;
        }

        entity.WorkflowState = data.WorkflowState;
    }
}