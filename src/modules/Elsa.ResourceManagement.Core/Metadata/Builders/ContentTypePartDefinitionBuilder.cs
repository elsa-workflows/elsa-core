using System.Text.Json.Nodes;
using Elsa.ResourceManagement.Metadata.Models;
using Elsa.ResourceManagement.Serialization.Extensions;

namespace Elsa.ResourceManagement.Metadata.Builders;

public abstract class ContentTypePartDefinitionBuilder(ContentTypePartDefinition part)
{
    protected readonly JsonObject Settings = part.Settings?.Clone() ?? [];

    public ContentTypePartDefinition Current { get; protected set; } = part;
    public string Name { get; private set; } = part.Name;
    public string PartName { get; private set; } = part.PartDefinition.Name;
    public string TypeName { get; private set; } = part.ContentTypeDefinition != null! ? part.ContentTypeDefinition.Name : string.Empty;

    public ContentTypePartDefinitionBuilder WithSettings<T>(T settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var jObject = JsonObjectEx.FromObject(settings, ResourceBuilderSettings.IgnoreDefaultValuesSerializer);
        Settings[typeof(T).Name] = jObject;

        return this;
    }

    public ContentTypePartDefinitionBuilder MergeSettings(JsonObject settings)
    {
        Settings.Merge(settings, ResourceBuilderSettings.JsonMergeSettings);
        return this;
    }

    public ContentTypePartDefinitionBuilder MergeSettings<T>(Action<T> setting) where T : class, new()
    {
        var existingJObject = Settings[typeof(T).Name] as JsonObject;
        
        if (existingJObject == null)
        {
            existingJObject = JsonObjectEx.FromObject(new T(), ResourceBuilderSettings.IgnoreDefaultValuesSerializer);
            Settings[typeof(T).Name] = existingJObject;
        }

        var settingsToMerge = existingJObject.ToObject<T>()!;
        setting(settingsToMerge);
        Settings[typeof(T).Name] = JsonObjectEx.FromObject(settingsToMerge, ResourceBuilderSettings.IgnoreDefaultValuesSerializer);
        return this;
    }

    public abstract ContentTypePartDefinition Build();
}