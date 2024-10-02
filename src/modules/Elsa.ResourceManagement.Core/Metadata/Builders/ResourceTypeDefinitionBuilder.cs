using System.Text.Json.Nodes;
using Elsa.ResourceManagement.Metadata.Models;
using Elsa.ResourceManagement.Serialization.Extensions;

namespace Elsa.ResourceManagement.Metadata.Builders;

public class ResourceTypeDefinitionBuilder
{
    private string _name = string.Empty;
    private string _displayName = string.Empty;
    private readonly List<ResourceTypePartDefinition> _parts;
    private readonly JsonObject _settings;

    public ResourceTypeDefinition Current { get; }

    public ResourceTypeDefinitionBuilder() : this(new ResourceTypeDefinition(string.Empty, string.Empty))
    {
    }

    public ResourceTypeDefinitionBuilder(ResourceTypeDefinition? existing)
    {
        Current = existing ?? new ResourceTypeDefinition(string.Empty, string.Empty);

        if (existing == null)
        {
            _parts = [];
            _settings = [];
        }
        else
        {
            _name = existing.Name;
            _displayName = existing.DisplayName;
            _parts = existing.Parts.ToList();
            _settings = existing.Settings?.Clone() ?? [];
        }
    }

    public ResourceTypeDefinition Build()
    {
        if (!char.IsLetter(_name[0]))
            throw new InvalidOperationException("Resource type name must start with a letter");
        if (!string.Equals(_name, _name.ToSafeName(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Resource type name contains invalid characters");
        if (_name.IsReservedResourceName())
            throw new InvalidOperationException("Resource type name is reserved for internal use");

        return new ResourceTypeDefinition(_name, _displayName, _parts, _settings);
    }

    public ResourceTypeDefinitionBuilder Named(string name)
    {
        _name = name;
        return this;
    }

    public ResourceTypeDefinitionBuilder DisplayedAs(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    public ResourceTypeDefinitionBuilder MergeSettings(JsonObject settings)
    {
        _settings.Merge(settings, ResourceBuilderSettings.JsonMergeSettings);
        return this;
    }

    public ResourceTypeDefinitionBuilder MergeSettings<T>(Action<T> mergeAction) where T : class, new()
    {
        _settings.Merge(mergeAction);
        return this;
    }

    public ResourceTypeDefinitionBuilder WithSettings<T>(T settings)
    {
        _settings.SetProperty(settings);
        return this;
    }

    public ResourceTypeDefinitionBuilder RemovePart(string partName)
    {
        var existingPart = _parts.SingleOrDefault(x => string.Equals(x.Name, partName, StringComparison.OrdinalIgnoreCase));
        if (existingPart != null)
        {
            _parts.Remove(existingPart);
        }

        return this;
    }

    public ResourceTypeDefinitionBuilder WithPart(string partName) => WithPart(partName, configuration => { });
    public ResourceTypeDefinitionBuilder WithPart(string name, string partName) => WithPart(name, new ResourcePartDefinition(partName), configuration => { });
    public ResourceTypeDefinitionBuilder WithPart(string partName, Action<ResourceTypePartDefinitionBuilder> configuration) => WithPart(partName, new ResourcePartDefinition(partName), configuration);

    public ResourceTypeDefinitionBuilder WithPart(string name, ResourcePartDefinition partDefinition, Action<ResourceTypePartDefinitionBuilder> configuration)
    {
        var existingPart = _parts.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existingPart != null)
        {
            _parts.Remove(existingPart);
        }
        else
        {
            existingPart = new ResourceTypePartDefinition(name, partDefinition, [])
            {
                ResourceTypeDefinition = Current,
            };
        }

        var configurer = new PartConfigurerImpl(existingPart);
        configuration(configurer);
        _parts.Add(configurer.Build());
        return this;
    }

    public ResourceTypeDefinitionBuilder WithPart(string name, string partName, Action<ResourceTypePartDefinitionBuilder> configuration) => WithPart(name, new ResourcePartDefinition(partName), configuration);
    public ResourceTypeDefinitionBuilder WithPart<TPart>() where TPart : ResourcePart => WithPart(typeof(TPart).Name, configuration => { });
    public ResourceTypeDefinitionBuilder WithPart<TPart>(string name) where TPart : ResourcePart => WithPart(name, new ResourcePartDefinition(typeof(TPart).Name), configuration => { });
    public ResourceTypeDefinitionBuilder WithPart<TPart>(string name, Action<ResourceTypePartDefinitionBuilder> configuration) where TPart : ResourcePart => WithPart(name, new ResourcePartDefinition(typeof(TPart).Name), configuration);
    public ResourceTypeDefinitionBuilder WithPart<TPart>(Action<ResourceTypePartDefinitionBuilder> configuration) where TPart : ResourcePart => WithPart(typeof(TPart).Name, configuration);
    public Task<ResourceTypeDefinitionBuilder> WithPartAsync(string name, string partName, Func<ResourceTypePartDefinitionBuilder, Task> configurationAsync) => WithPartAsync(name, new ResourcePartDefinition(partName), configurationAsync);
    public Task<ResourceTypeDefinitionBuilder> WithPartAsync(string partName, Func<ResourceTypePartDefinitionBuilder, Task> configurationAsync) => WithPartAsync(partName, new ResourcePartDefinition(partName), configurationAsync);

    public async Task<ResourceTypeDefinitionBuilder> WithPartAsync(string name, ResourcePartDefinition partDefinition, Func<ResourceTypePartDefinitionBuilder, Task> configurationAsync)
    {
        var existingPart = _parts.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

        if (existingPart != null)
            _parts.Remove(existingPart);
        else
        {
            existingPart = new ResourceTypePartDefinition(name, partDefinition, [])
            {
                ResourceTypeDefinition = Current,
            };
        }

        var configurer = new PartConfigurerImpl(existingPart);

        await configurationAsync(configurer);

        _parts.Add(configurer.Build());

        return this;
    }

    private sealed class PartConfigurerImpl : ResourceTypePartDefinitionBuilder
    {
        private readonly ResourcePartDefinition _partDefinition;

        public PartConfigurerImpl(ResourceTypePartDefinition part) : base(part)
        {
            Current = part;
            _partDefinition = part.PartDefinition;
        }

        public override ResourceTypePartDefinition Build()
        {
            if (!char.IsLetter(Current.Name[0]))
                throw new InvalidOperationException("Resource part name must start with a letter");

            if (!string.Equals(Current.Name, Current.Name.ToSafeName(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Resource part name contains invalid characters");

            return new ResourceTypePartDefinition(Current.Name, _partDefinition, Settings)
            {
                ResourceTypeDefinition = Current.ResourceTypeDefinition,
            };
        }
    }
}