using System.Text.Json.Nodes;
using Elsa.ResourceManagement.Metadata.Models;
using Elsa.ResourceManagement.Serialization.Extensions;

namespace Elsa.ResourceManagement.Metadata.Builders;

public class ContentTypeDefinitionBuilder
{
    private string _name = string.Empty;
    private string _displayName = string.Empty;
    private readonly List<ContentTypePartDefinition> _parts;
    private readonly JsonObject _settings;

    public ContentTypeDefinition Current { get; }

    public ContentTypeDefinitionBuilder() : this(new ContentTypeDefinition(string.Empty, string.Empty))
    {
    }

    public ContentTypeDefinitionBuilder(ContentTypeDefinition? existing)
    {
        Current = existing ?? new ContentTypeDefinition(string.Empty, string.Empty);

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

    public ContentTypeDefinition Build()
    {
        if (!char.IsLetter(_name[0]))
            throw new InvalidOperationException("Content type name must start with a letter");
        if (!string.Equals(_name, _name.ToSafeName(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Content type name contains invalid characters");
        if (_name.IsReservedResourceName())
            throw new InvalidOperationException("Content type name is reserved for internal use");

        return new ContentTypeDefinition(_name, _displayName, _parts, _settings);
    }

    public ContentTypeDefinitionBuilder Named(string name)
    {
        _name = name;
        return this;
    }

    public ContentTypeDefinitionBuilder DisplayedAs(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    public ContentTypeDefinitionBuilder MergeSettings(JsonObject settings)
    {
        _settings.Merge(settings, ResourceBuilderSettings.JsonMergeSettings);
        return this;
    }

    public ContentTypeDefinitionBuilder MergeSettings<T>(Action<T> mergeAction) where T : class, new()
    {
        _settings.Merge(mergeAction);
        return this;
    }

    public ContentTypeDefinitionBuilder WithSettings<T>(T settings)
    {
        _settings.SetProperty(settings);
        return this;
    }

    public ContentTypeDefinitionBuilder RemovePart(string partName)
    {
        var existingPart = _parts.SingleOrDefault(x => string.Equals(x.Name, partName, StringComparison.OrdinalIgnoreCase));
        if (existingPart != null)
        {
            _parts.Remove(existingPart);
        }

        return this;
    }

    public ContentTypeDefinitionBuilder WithPart(string partName) => WithPart(partName, configuration => { });
    public ContentTypeDefinitionBuilder WithPart(string name, string partName) => WithPart(name, new ContentPartDefinition(partName), configuration => { });
    public ContentTypeDefinitionBuilder WithPart(string partName, Action<ContentTypePartDefinitionBuilder> configuration) => WithPart(partName, new ContentPartDefinition(partName), configuration);

    public ContentTypeDefinitionBuilder WithPart(string name, ContentPartDefinition partDefinition, Action<ContentTypePartDefinitionBuilder> configuration)
    {
        var existingPart = _parts.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existingPart != null)
        {
            _parts.Remove(existingPart);
        }
        else
        {
            existingPart = new ContentTypePartDefinition(name, partDefinition, [])
            {
                ContentTypeDefinition = Current,
            };
        }

        var configurer = new PartConfigurerImpl(existingPart);
        configuration(configurer);
        _parts.Add(configurer.Build());
        return this;
    }

    public ContentTypeDefinitionBuilder WithPart(string name, string partName, Action<ContentTypePartDefinitionBuilder> configuration) => WithPart(name, new ContentPartDefinition(partName), configuration);
    public ContentTypeDefinitionBuilder WithPart<TPart>() where TPart : ResourcePart => WithPart(typeof(TPart).Name, configuration => { });
    public ContentTypeDefinitionBuilder WithPart<TPart>(string name) where TPart : ResourcePart => WithPart(name, new ContentPartDefinition(typeof(TPart).Name), configuration => { });
    public ContentTypeDefinitionBuilder WithPart<TPart>(string name, Action<ContentTypePartDefinitionBuilder> configuration) where TPart : ResourcePart => WithPart(name, new ContentPartDefinition(typeof(TPart).Name), configuration);
    public ContentTypeDefinitionBuilder WithPart<TPart>(Action<ContentTypePartDefinitionBuilder> configuration) where TPart : ResourcePart => WithPart(typeof(TPart).Name, configuration);
    public Task<ContentTypeDefinitionBuilder> WithPartAsync(string name, string partName, Func<ContentTypePartDefinitionBuilder, Task> configurationAsync) => WithPartAsync(name, new ContentPartDefinition(partName), configurationAsync);
    public Task<ContentTypeDefinitionBuilder> WithPartAsync(string partName, Func<ContentTypePartDefinitionBuilder, Task> configurationAsync) => WithPartAsync(partName, new ContentPartDefinition(partName), configurationAsync);

    public async Task<ContentTypeDefinitionBuilder> WithPartAsync(string name, ContentPartDefinition partDefinition, Func<ContentTypePartDefinitionBuilder, Task> configurationAsync)
    {
        var existingPart = _parts.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

        if (existingPart != null)
            _parts.Remove(existingPart);
        else
        {
            existingPart = new ContentTypePartDefinition(name, partDefinition, [])
            {
                ContentTypeDefinition = Current,
            };
        }

        var configurer = new PartConfigurerImpl(existingPart);

        await configurationAsync(configurer);

        _parts.Add(configurer.Build());

        return this;
    }

    private sealed class PartConfigurerImpl : ContentTypePartDefinitionBuilder
    {
        private readonly ContentPartDefinition _partDefinition;

        public PartConfigurerImpl(ContentTypePartDefinition part) : base(part)
        {
            Current = part;
            _partDefinition = part.PartDefinition;
        }

        public override ContentTypePartDefinition Build()
        {
            if (!char.IsLetter(Current.Name[0]))
                throw new InvalidOperationException("Content part name must start with a letter");

            if (!string.Equals(Current.Name, Current.Name.ToSafeName(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Content part name contains invalid characters");

            return new ContentTypePartDefinition(Current.Name, _partDefinition, Settings)
            {
                ContentTypeDefinition = Current.ContentTypeDefinition,
            };
        }
    }
}