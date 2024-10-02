using System.Text.Json.Nodes;
using Elsa.ResourceManagement.Metadata.Models;
using Elsa.ResourceManagement.Serialization.Extensions;

namespace Elsa.ResourceManagement.Metadata.Builders;

public abstract class ResourcePartFieldDefinitionBuilder(ResourcePartFieldDefinition field)
{
    protected readonly JsonObject Settings = field.Settings?.Clone() ?? new JsonObject();

    public ResourcePartFieldDefinition Current { get; private set; } = field;
    public abstract string Name { get; }
    public abstract string FieldType { get; }
    public abstract string PartName { get; }

    public ResourcePartFieldDefinitionBuilder MergeSettings(JsonObject settings)
    {
        Settings.Merge(settings, ResourceBuilderSettings.JsonMergeSettings);
        return this;
    }

    public ResourcePartFieldDefinitionBuilder MergeSettings<T>(Action<T> setting) where T : class, new()
    {
        var existingJObject = Settings[typeof(T).Name] as JsonObject;
        
        if (existingJObject == null)
        {
            existingJObject = ToJsonObject(new T());
            Settings[typeof(T).Name] = existingJObject;
        }

        var settingsToMerge = existingJObject.ToObject<T>()!;
        setting(settingsToMerge);
        Settings[typeof(T).Name] = ToJsonObject(settingsToMerge);
        return this;
    }

    public ResourcePartFieldDefinitionBuilder WithSettings<T>(T settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Settings[typeof(T).Name] = ToJsonObject(settings);

        return this;
    }

    public ResourcePartFieldDefinitionBuilder WithSettings<T>() where T : class, new()
    {
        Settings[typeof(T).Name] = ToJsonObject(new T());

        return this;
    }

    public abstract ResourcePartFieldDefinitionBuilder OfType(ResourceFieldDefinition fieldDefinition);
    public abstract ResourcePartFieldDefinitionBuilder OfType(string fieldType);
    public abstract ResourcePartFieldDefinition Build();

    private static JsonObject ToJsonObject(object obj) => JsonObjectEx.FromObject(obj, ResourceBuilderSettings.IgnoreDefaultValuesSerializer)!;
}