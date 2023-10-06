using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.Common.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Serialization;

/// <summary>
/// A base class for configurable JSON serializers.
/// </summary>
public abstract class ConfigurableSerializer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurableSerializer"/> class.
    /// </summary>
    protected ConfigurableSerializer(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
    
    /// <summary>
    /// Gets the service provider.
    /// </summary>
    protected IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Creates a new instance of <see cref="JsonSerializerOptions"/> with the configured options. 
    /// </summary>
    protected virtual JsonSerializerOptions CreateOptions()
    {
        var options = CreateOptionsInternal();
        Apply(options);
        return options;
    }
    
    /// <summary>
    /// Creates a new instance of <see cref="JsonSerializerOptions"/> with the configured options. 
    /// </summary>
    protected virtual void Apply(JsonSerializerOptions options)
    {
        Configure(options);
        AddConverters(options);
        RunConfigurators(options);
    }

    /// <summary>
    /// Creates a new instance of <see cref="JsonSerializerOptions"/>.
    /// </summary>
    private JsonSerializerOptions CreateOptionsInternal()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(JsonMetadataServices.TimeSpanConverter);

        return options;
    }

    /// <summary>
    /// Configures the <see cref="JsonSerializerOptions"/> object.
    /// </summary>
    protected virtual void Configure(JsonSerializerOptions options)
    {
    }

    /// <summary>
    /// Adds additional <see cref="JsonConverter"/> objects.
    /// </summary>
    protected virtual void AddConverters(JsonSerializerOptions options)
    {
    }

    /// <summary>
    /// Give external packages a chance to further configure the serializer options.
    /// </summary>
    protected virtual void RunConfigurators(JsonSerializerOptions options)
    {
        var configurators = ServiceProvider.GetServices<ISerializationOptionsConfigurator>();
        var modifiers = new List<Action<JsonTypeInfo>>();
        
        foreach (var configurator in configurators)
        {
            configurator.Configure(options);
            var modifiersToAdd = configurator.GetModifiers();
            modifiers.AddRange(modifiersToAdd);
        }
        
        options.TypeInfoResolver = new ModifiableJsonTypeInfoResolver(modifiers);
    }

    /// <summary>
    /// Creates an instance of the specified type using the service provider.
    /// </summary>
    protected T CreateInstance<T>() => ActivatorUtilities.CreateInstance<T>(ServiceProvider);
}