using System.Text.Json.Nodes;
using Elsa.ResourceManagement.Metadata.Models;
using Elsa.ResourceManagement.Serialization.Extensions;

namespace Elsa.ResourceManagement.Metadata.Builders;

public abstract class ContentPartFieldDefinitionBuilder(ContentPartFieldDefinition field)
{
    protected readonly JsonObject Settings = field.Settings?.Clone() ?? new JsonObject();

    public ContentPartFieldDefinition Current { get; private set; } = field;
    public abstract string Name { get; }
    public abstract string FieldType { get; }
    public abstract string PartName { get; }

    public ContentPartFieldDefinitionBuilder MergeSettings(JsonObject settings)
    {
        Settings.Merge(settings, ResourceBuilderSettings.JsonMergeSettings);
        return this;
    }

    public ContentPartFieldDefinitionBuilder MergeSettings<T>(Action<T> setting) where T : class, new()
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

    public ContentPartFieldDefinitionBuilder WithSettings<T>(T settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Settings[typeof(T).Name] = ToJsonObject(settings);

        return this;
    }

    public ContentPartFieldDefinitionBuilder WithSettings<T>() where T : class, new()
    {
        Settings[typeof(T).Name] = ToJsonObject(new T());

        return this;
    }

    public abstract ContentPartFieldDefinitionBuilder OfType(ContentFieldDefinition fieldDefinition);
    public abstract ContentPartFieldDefinitionBuilder OfType(string fieldType);
    public abstract ContentPartFieldDefinition Build();

    private static JsonObject ToJsonObject(object obj) => JsonObjectEx.FromObject(obj, ResourceBuilderSettings.IgnoreDefaultValuesSerializer)!;
}