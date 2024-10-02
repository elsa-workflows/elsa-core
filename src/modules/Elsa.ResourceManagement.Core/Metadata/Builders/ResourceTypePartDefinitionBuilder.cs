using System.Text.Json.Nodes;
using Elsa.ResourceManagement.Metadata.Models;
using Elsa.ResourceManagement.Serialization.Extensions;

namespace Elsa.ResourceManagement.Metadata.Builders;

public abstract class ResourceTypePartDefinitionBuilder(ResourceTypePartDefinition part)
{
    protected readonly JsonObject Settings = part.Settings?.Clone() ?? [];

    public ResourceTypePartDefinition Current { get; protected set; } = part;
    public string Name { get; private set; } = part.Name;
    public string PartName { get; private set; } = part.PartDefinition.Name;
    public string TypeName { get; private set; } = part.ResourceTypeDefinition != null! ? part.ResourceTypeDefinition.Name : string.Empty;

    public ResourceTypePartDefinitionBuilder WithSettings<T>(T settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var jObject = JsonObjectEx.FromObject(settings, ResourceBuilderSettings.IgnoreDefaultValuesSerializer);
        Settings[typeof(T).Name] = jObject;

        return this;
    }

    public ResourceTypePartDefinitionBuilder MergeSettings(JsonObject settings)
    {
        Settings.Merge(settings, ResourceBuilderSettings.JsonMergeSettings);
        return this;
    }

    public ResourceTypePartDefinitionBuilder MergeSettings<T>(Action<T> setting) where T : class, new()
    {
        Settings.Merge(setting);
        return this;
    }

    public abstract ResourceTypePartDefinition Build();
}