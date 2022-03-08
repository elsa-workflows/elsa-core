using System.Text.Json;
using Elsa.Contracts;
using Elsa.Management.Serialization;
using Elsa.Models;
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
            entity.Variables,
            entity.ApplicationProperties
        };

        var options = _workflowSerializerOptionsProvider.CreatePersistenceOptions();
        var json = JsonSerializer.Serialize(data, options);

        dbContext.Entry(entity).Property("Data").CurrentValue = json;
    }

    public void Deserialize(ElsaDbContext dbContext, WorkflowDefinition entity)
    {
        var data = new WorkflowDefinitionState(entity.Root, entity.Variables, entity.ApplicationProperties);
        var json = (string?) dbContext.Entry(entity).Property("Data").CurrentValue;

        if (!string.IsNullOrWhiteSpace(json))
        {
            var options = _workflowSerializerOptionsProvider.CreatePersistenceOptions();
            data = JsonSerializer.Deserialize<WorkflowDefinitionState>(json, options)!;
        }

        entity.Root = data.Root;
        entity.Variables = data.Variables;
        entity.ApplicationProperties = data.ApplicationProperties;
    }
    
    // Can't use records when using System.Text.Json serialization and reference handling. Hence, using a class with default constructor.
    private class WorkflowDefinitionState
    {
        public WorkflowDefinitionState()
        {
        }

        public WorkflowDefinitionState(IActivity root, ICollection<Variable> variables, IDictionary<string, object> applicationProperties)
        {
            Root = root;
            Variables = variables;
            ApplicationProperties = applicationProperties;
        }
        
        public IActivity Root { get; init; } = default!;
        public ICollection<Variable> Variables { get; set; } = new List<Variable>();
        public IDictionary<string, object> ApplicationProperties { get; set; } = new Dictionary<string, object>();
    }
}