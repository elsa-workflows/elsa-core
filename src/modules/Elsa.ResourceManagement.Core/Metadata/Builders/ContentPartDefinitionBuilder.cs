using System.Text.Json.Nodes;
using Elsa.ResourceManagement.Metadata.Models;
using Elsa.ResourceManagement.Metadata.Settings;
using Elsa.ResourceManagement.Serialization.Extensions;

namespace Elsa.ResourceManagement.Metadata.Builders;

public class ContentPartDefinitionBuilder
{
    private readonly ContentPartDefinition? _part;
    private readonly List<ContentPartFieldDefinition> _fields;
    private readonly JsonObject _settings;

    public ContentPartDefinition Current { get; private set; }

    public ContentPartDefinitionBuilder() : this(new ContentPartDefinition(null))
    {
    }

    public ContentPartDefinitionBuilder(ContentPartDefinition? existing)
    {
        _part = existing;

        if (existing == null)
        {
            _fields = [];
            _settings = [];
        }
        else
        {
            Name = existing.Name;
            _fields = existing.Fields.ToList();
            _settings = existing.Settings == null ? [] : existing.Settings.Clone();
        }
    }

    public string? Name { get; private set; }

    public ContentPartDefinition Build()
    {
        if(Name == null)
            throw new InvalidOperationException("Name is required");
            
        if (!char.IsLetter(Name[0]))
            throw new InvalidOperationException("Content part name must start with a letter");
            
        if (!string.Equals(Name, Name.ToSafeName(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Content part name contains invalid characters");
            
        if (Name.IsReservedResourceName())
            throw new InvalidOperationException("Content part name is reserved for internal use");

        return new ContentPartDefinition(Name, _fields, _settings);
    }

    public ContentPartDefinitionBuilder Named(string name)
    {
        Name = name;

        return this;
    }

    public ContentPartDefinitionBuilder RemoveField(string fieldName)
    {
        var existingField = _fields.SingleOrDefault(x => string.Equals(x.Name, fieldName, StringComparison.OrdinalIgnoreCase));
        if (existingField != null) 
            _fields.Remove(existingField);

        return this;
    }

    public ContentPartDefinitionBuilder MergeSettings(JsonObject settings)
    {
        _settings.Merge(settings, ResourceBuilderSettings.JsonMergeSettings);

        return this;
    }

    public ContentPartDefinitionBuilder MergeSettings<T>(Action<T> mergeAction) where T : class, new()
    {
        _settings.Merge(mergeAction);
        return this;
    }

    public ContentPartDefinitionBuilder WithSettings<T>(T settings)
    {
        _settings.SetProperty(settings);
        return this;
    }

    public ContentPartDefinitionBuilder WithField(string fieldName) => WithField(fieldName, configuration => { });

    public ContentPartDefinitionBuilder WithField(string fieldName, Action<ContentPartFieldDefinitionBuilder> configuration)
    {
        var existingField = _fields.FirstOrDefault(x => string.Equals(x.Name, fieldName, StringComparison.OrdinalIgnoreCase));
        if (existingField != null)
        {
            var toRemove = _fields.Where(x => x.Name == fieldName).ToArray();
            foreach (var remove in toRemove)
            {
                _fields.Remove(remove);
            }
        }
        else
        {
            existingField = new ContentPartFieldDefinition(null, fieldName, []);
        }

        var configurer = new FieldConfigurerImpl(existingField, _part);

        configuration(configurer);

        var fieldDefinition = configurer.Build();

        var settings = fieldDefinition.GetSettings<ContentPartFieldSettings>();

        if (string.IsNullOrEmpty(settings.DisplayName))
        {
            // If there is no display name, let's use the field name by default.
            settings.DisplayName = fieldName;
            fieldDefinition.Settings?.Remove(nameof(ContentPartFieldSettings));
            fieldDefinition.Settings?.Add(nameof(ContentPartFieldSettings), JsonNodeEx.FromObject(settings));
        }

        _fields.Add(fieldDefinition);

        return this;
    }

    public ContentPartDefinitionBuilder WithField<TField>(string fieldName) => WithField(fieldName, configuration => configuration.OfType(typeof(TField).Name));

    public ContentPartDefinitionBuilder WithField<TField>(string fieldName, Action<ContentPartFieldDefinitionBuilder> configuration)
    {
        return WithField(fieldName, field =>
        {
            configuration(field);
            field.OfType(typeof(TField).Name);
        });
    }

    public Task<ContentPartDefinitionBuilder> WithFieldAsync<TField>(string fieldName, Func<ContentPartFieldDefinitionBuilder, Task> configuration)
    {
        return WithFieldAsync(fieldName, async field =>
        {
            await configuration(field);
            field.OfType(typeof(TField).Name);
        });
    }

    public async Task<ContentPartDefinitionBuilder> WithFieldAsync(string fieldName, Func<ContentPartFieldDefinitionBuilder, Task> configurationAsync)
    {
        var existingField = _fields.FirstOrDefault(x => string.Equals(x.Name, fieldName, StringComparison.OrdinalIgnoreCase));

        if (existingField != null)
        {
            var toRemove = _fields.Where(x => x.Name == fieldName).ToArray();
            foreach (var remove in toRemove)
            {
                _fields.Remove(remove);
            }
        }
        else
        {
            existingField = new ContentPartFieldDefinition(null, fieldName, []);
        }

        var configurer = new FieldConfigurerImpl(existingField, _part);
        await configurationAsync(configurer);

        var fieldDefinition = configurer.Build();

        var settings = fieldDefinition.GetSettings<ContentPartFieldSettings>();

        if (string.IsNullOrEmpty(settings.DisplayName))
        {
            // If there is no display name, let's use the field name by default.
            settings.DisplayName = fieldName;
            fieldDefinition.Settings?.Remove(nameof(ContentPartFieldSettings));
            fieldDefinition.Settings?.Add(nameof(ContentPartFieldSettings), JsonNodeEx.FromObject(settings));
        }

        _fields.Add(fieldDefinition);

        return this;
    }

    private sealed class FieldConfigurerImpl(ContentPartFieldDefinition field, ContentPartDefinition part) : ContentPartFieldDefinitionBuilder(field)
    {
        private ContentFieldDefinition _fieldDefinition = field.FieldDefinition;
        private readonly string _fieldName = field.Name;

        public override ContentPartFieldDefinition Build()
        {
            if (!char.IsLetter(_fieldName[0]))
                throw new InvalidOperationException("Content field name must start with a letter");
            if (!string.Equals(_fieldName, _fieldName.ToSafeName(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Content field name contains invalid characters");

            return new ContentPartFieldDefinition(_fieldDefinition, _fieldName, Settings);
        }

        public override string Name => _fieldName;
        public override string FieldType => _fieldDefinition.Name;
        public override string PartName => part.Name;

        public override ContentPartFieldDefinitionBuilder OfType(ContentFieldDefinition fieldDefinition)
        {
            _fieldDefinition = fieldDefinition;
            return this;
        }

        public override ContentPartFieldDefinitionBuilder OfType(string fieldType)
        {
            _fieldDefinition = new ContentFieldDefinition(fieldType);
            return this;
        }
    }
}