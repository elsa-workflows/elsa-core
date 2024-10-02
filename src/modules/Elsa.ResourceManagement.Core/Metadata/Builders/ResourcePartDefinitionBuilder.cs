using System.Text.Json.Nodes;
using Elsa.ResourceManagement.Metadata.Models;
using Elsa.ResourceManagement.Metadata.Settings;
using Elsa.ResourceManagement.Serialization.Extensions;

namespace Elsa.ResourceManagement.Metadata.Builders;

public class ResourcePartDefinitionBuilder
{
    private readonly ResourcePartDefinition? _part;
    private readonly List<ResourcePartFieldDefinition> _fields;
    private readonly JsonObject _settings;

    public ResourcePartDefinition Current { get; private set; }

    public ResourcePartDefinitionBuilder() : this(new ResourcePartDefinition(null))
    {
    }

    public ResourcePartDefinitionBuilder(ResourcePartDefinition? existing)
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

    public ResourcePartDefinition Build()
    {
        if(Name == null)
            throw new InvalidOperationException("Name is required");
            
        if (!char.IsLetter(Name[0]))
            throw new InvalidOperationException("Resource part name must start with a letter");
            
        if (!string.Equals(Name, Name.ToSafeName(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Resource part name contains invalid characters");
            
        if (Name.IsReservedResourceName())
            throw new InvalidOperationException("Resource part name is reserved for internal use");

        return new ResourcePartDefinition(Name, _fields, _settings);
    }

    public ResourcePartDefinitionBuilder Named(string name)
    {
        Name = name;

        return this;
    }

    public ResourcePartDefinitionBuilder RemoveField(string fieldName)
    {
        var existingField = _fields.SingleOrDefault(x => string.Equals(x.Name, fieldName, StringComparison.OrdinalIgnoreCase));
        if (existingField != null) 
            _fields.Remove(existingField);

        return this;
    }

    public ResourcePartDefinitionBuilder MergeSettings(JsonObject settings)
    {
        _settings.Merge(settings, ResourceBuilderSettings.JsonMergeSettings);

        return this;
    }

    public ResourcePartDefinitionBuilder MergeSettings<T>(Action<T> mergeAction) where T : class, new()
    {
        _settings.Merge(mergeAction);
        return this;
    }

    public ResourcePartDefinitionBuilder WithSettings<T>(T settings)
    {
        _settings.SetProperty(settings);
        return this;
    }

    public ResourcePartDefinitionBuilder WithField(string fieldName) => WithField(fieldName, configuration => { });

    public ResourcePartDefinitionBuilder WithField(string fieldName, Action<ResourcePartFieldDefinitionBuilder> configuration)
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
            existingField = new ResourcePartFieldDefinition(null, fieldName, []);
        }

        var configurer = new FieldConfigurerImpl(existingField, _part);

        configuration(configurer);

        var fieldDefinition = configurer.Build();

        var settings = fieldDefinition.GetSettings<ResourcePartFieldSettings>();

        if (string.IsNullOrEmpty(settings.DisplayName))
        {
            // If there is no display name, let's use the field name by default.
            settings.DisplayName = fieldName;
            fieldDefinition.Settings?.Remove(nameof(ResourcePartFieldSettings));
            fieldDefinition.Settings?.Add(nameof(ResourcePartFieldSettings), JsonNodeEx.FromObject(settings));
        }

        _fields.Add(fieldDefinition);

        return this;
    }

    public ResourcePartDefinitionBuilder WithField<TField>(string fieldName) => WithField(fieldName, configuration => configuration.OfType(typeof(TField).Name));

    public ResourcePartDefinitionBuilder WithField<TField>(string fieldName, Action<ResourcePartFieldDefinitionBuilder> configuration)
    {
        return WithField(fieldName, field =>
        {
            configuration(field);
            field.OfType(typeof(TField).Name);
        });
    }

    public Task<ResourcePartDefinitionBuilder> WithFieldAsync<TField>(string fieldName, Func<ResourcePartFieldDefinitionBuilder, Task> configuration)
    {
        return WithFieldAsync(fieldName, async field =>
        {
            await configuration(field);
            field.OfType(typeof(TField).Name);
        });
    }

    public async Task<ResourcePartDefinitionBuilder> WithFieldAsync(string fieldName, Func<ResourcePartFieldDefinitionBuilder, Task> configurationAsync)
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
            existingField = new ResourcePartFieldDefinition(null, fieldName, []);
        }

        var configurer = new FieldConfigurerImpl(existingField, _part);
        await configurationAsync(configurer);

        var fieldDefinition = configurer.Build();

        var settings = fieldDefinition.GetSettings<ResourcePartFieldSettings>();

        if (string.IsNullOrEmpty(settings.DisplayName))
        {
            // If there is no display name, let's use the field name by default.
            settings.DisplayName = fieldName;
            fieldDefinition.Settings?.Remove(nameof(ResourcePartFieldSettings));
            fieldDefinition.Settings?.Add(nameof(ResourcePartFieldSettings), JsonNodeEx.FromObject(settings));
        }

        _fields.Add(fieldDefinition);

        return this;
    }

    private sealed class FieldConfigurerImpl(ResourcePartFieldDefinition field, ResourcePartDefinition part) : ResourcePartFieldDefinitionBuilder(field)
    {
        private ResourceFieldDefinition _fieldDefinition = field.FieldDefinition;
        private readonly string _fieldName = field.Name;

        public override ResourcePartFieldDefinition Build()
        {
            if (!char.IsLetter(_fieldName[0]))
                throw new InvalidOperationException("Resource field name must start with a letter");
            if (!string.Equals(_fieldName, _fieldName.ToSafeName(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Resource field name contains invalid characters");

            return new ResourcePartFieldDefinition(_fieldDefinition, _fieldName, Settings);
        }

        public override string Name => _fieldName;
        public override string FieldType => _fieldDefinition.Name;
        public override string PartName => part.Name;

        public override ResourcePartFieldDefinitionBuilder OfType(ResourceFieldDefinition fieldDefinition)
        {
            _fieldDefinition = fieldDefinition;
            return this;
        }

        public override ResourcePartFieldDefinitionBuilder OfType(string fieldType)
        {
            _fieldDefinition = new ResourceFieldDefinition(fieldType);
            return this;
        }
    }
}