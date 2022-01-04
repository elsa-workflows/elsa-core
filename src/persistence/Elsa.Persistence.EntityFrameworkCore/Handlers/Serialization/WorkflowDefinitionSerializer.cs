using System.Text.Json;
using Elsa.Management.Serialization;
using Elsa.Management.Serialization.Extensions;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Contracts;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Serialization;

public class WorkflowDefinitionSerializer : IEntitySerializer<WorkflowDefinition>
{
    private readonly WorkflowSerializerOptionsProvider _workflowSerializerOptionsProvider;

    public WorkflowDefinitionSerializer(WorkflowSerializerOptionsProvider workflowSerializerOptionsProvider)
    {
        _workflowSerializerOptionsProvider = workflowSerializerOptionsProvider;
    }
    
    public void Serialize(ElsaDbContext dbContext, WorkflowDefinition entity)
    {
        var data = new
        {
            entity.Root,
            entity.Triggers
        };

        var options = _workflowSerializerOptionsProvider.CreateSerializerOptions();
        var json = JsonSerializer.Serialize(data, options);

        dbContext.Entry(entity).Property("Data").CurrentValue = json;
    }

    public void Deserialize(ElsaDbContext dbContext, WorkflowDefinition entity)
    {
        var data = new
        {
            entity.Root,
            entity.Triggers
        };

        var json = (string?) dbContext.Entry(entity).Property("Data").CurrentValue;

        if (!string.IsNullOrWhiteSpace(json))
        {
            var options = _workflowSerializerOptionsProvider.CreateSerializerOptions();
            data = JsonSerializerExtensions.DeserializeAnonymousType(json, data, options)!;
        }

        entity.Root = data.Root;
        entity.Triggers = data.Triggers;
    }
}