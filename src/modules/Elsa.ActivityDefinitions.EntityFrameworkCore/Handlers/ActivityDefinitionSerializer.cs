using System.Text.Json;
using Elsa.ActivityDefinitions.Entities;
using Elsa.Persistence.EntityFrameworkCore.Common.Services;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Serialization;

namespace Elsa.ActivityDefinitions.EntityFrameworkCore.Handlers;

public class ActivityDefinitionSerializer : IEntitySerializer<ActivityDefinitionsDbContext, ActivityDefinition>
{
    private readonly SerializerOptionsProvider _serializerOptionsProvider;

    public ActivityDefinitionSerializer(SerializerOptionsProvider serializerOptionsProvider)
    {
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    public void Serialize(ActivityDefinitionsDbContext dbContext, ActivityDefinition entity)
    {
        var data = new
        {
            entity.Variables,
            entity.Metadata,
            entity.ApplicationProperties
        };

        var options = _serializerOptionsProvider.CreatePersistenceOptions();
        var json = JsonSerializer.Serialize(data, options);

        dbContext.Entry(entity).Property("Data").CurrentValue = json;
    }

    public void Deserialize(ActivityDefinitionsDbContext dbContext, ActivityDefinition entity)
    {
        var data = new ActivityDefinitionState(entity.Variables, entity.Metadata, entity.ApplicationProperties);
        var json = (string?)dbContext.Entry(entity).Property("Data").CurrentValue;

        if (!string.IsNullOrWhiteSpace(json))
        {
            var options = _serializerOptionsProvider.CreatePersistenceOptions();
            data = JsonSerializer.Deserialize<ActivityDefinitionState>(json, options)!;
        }

        entity.Variables = data.Variables;
        entity.Metadata = data.Metadata;
        entity.ApplicationProperties = data.ApplicationProperties;
    }

    // Can't use records when using System.Text.Json serialization and reference handling. Hence, using a class with default constructor.
    private class ActivityDefinitionState
    {
        public ActivityDefinitionState()
        {
        }

        public ActivityDefinitionState(
            ICollection<Variable> variables,
            IDictionary<string, object> metadata,
            IDictionary<string, object> applicationProperties)
        {
            Variables = variables;
            Metadata = metadata;
            ApplicationProperties = applicationProperties;
        }

        public ICollection<Variable> Variables { get; set; } = new List<Variable>();
        public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public IDictionary<string, object> ApplicationProperties { get; set; } = new Dictionary<string, object>();
    }
}