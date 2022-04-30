using System.Text.Json;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Elsa.Persistence.Models;
using Elsa.Serialization;
using Elsa.State;

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
        var data = new WorkflowInstanceState(entity.WorkflowState, entity.Fault);
        var options = _workflowSerializerOptionsProvider.CreatePersistenceOptions();
        var json = JsonSerializer.Serialize(data, options);

        dbContext.Entry(entity).Property("Data").CurrentValue = json;
    }

    public void Deserialize(ElsaDbContext dbContext, WorkflowInstance entity)
    {
        var data = new WorkflowInstanceState(entity.WorkflowState, entity.Fault);
        var json = (string?)dbContext.Entry(entity).Property("Data").CurrentValue;

        if (!string.IsNullOrWhiteSpace(json))
        {
            var options = _workflowSerializerOptionsProvider.CreatePersistenceOptions();
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