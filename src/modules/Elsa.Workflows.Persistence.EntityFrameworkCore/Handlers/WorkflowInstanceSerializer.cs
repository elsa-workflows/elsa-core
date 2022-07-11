using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Persistence.EntityFrameworkCore.Common.Services;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Models;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Handlers;

public class WorkflowInstanceSerializer : IEntitySerializer<WorkflowsDbContext, WorkflowInstance>
{
    private readonly SerializerOptionsProvider _serializerOptionsProvider;

    public WorkflowInstanceSerializer(SerializerOptionsProvider serializerOptionsProvider)
    {
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    public void Serialize(WorkflowsDbContext dbContext, WorkflowInstance entity)
    {
        var data = new WorkflowInstanceState(entity.WorkflowState, entity.Fault);
        var options = _serializerOptionsProvider.CreatePersistenceOptions(ReferenceHandler.Preserve);
        var json = JsonSerializer.Serialize(data, options);

        dbContext.Entry(entity).Property("Data").CurrentValue = json;
    }

    public void Deserialize(WorkflowsDbContext dbContext, WorkflowInstance entity)
    {
        var data = new WorkflowInstanceState(entity.WorkflowState, entity.Fault);
        var json = (string?)dbContext.Entry(entity).Property("Data").CurrentValue;

        if (!string.IsNullOrWhiteSpace(json))
        {
            var options = _serializerOptionsProvider.CreatePersistenceOptions(ReferenceHandler.Preserve);
            data = JsonSerializer.Deserialize<WorkflowInstanceState>(json, options)!;
        }

        entity.WorkflowState = data.WorkflowState;
        entity.Fault = data.Fault;
    }

    // Can't use records when using System.Text.Json serialization and reference handling. Hence, using a class with default constructor.
    private class WorkflowInstanceState
    {
        public WorkflowInstanceState()
        {
        }

        public WorkflowInstanceState(WorkflowState workflowState, WorkflowFault? fault)
        {
            WorkflowState = workflowState;
            Fault = fault;
        }
        
        public WorkflowState WorkflowState { get; init; } = default!;
        public WorkflowFault? Fault { get; set; }
    }
}