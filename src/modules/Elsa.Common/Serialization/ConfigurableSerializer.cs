using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Unicode;
using Elsa.Common.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Serialization;

/// <summary>
/// A base class for configurable JSON serializers.
/// </summary>
public abstract class ConfigurableSerializer
{
    private JsonSerializerOptions? _options;

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
    public virtual JsonSerializerOptions GetOptions()
    {
        if (_options != null)
            return _options;

        var options = CreateOptionsInternal();
        ApplyOptions(options);
        _options = options;
        return options;
    }

    /// <summary>
    /// Creates a new instance of <see cref="JsonSerializerOptions"/> with the configured options. 
    /// </summary>
    public virtual void ApplyOptions(JsonSerializerOptions options)
    {
        Configure(options);
        AddConverters(options);
        RunConfigurators(options);
    }
    
    /// <summary>
    /// Creates a new instance of <see cref="JsonSerializerOptions"/>.
    /// </summary>
    protected JsonSerializerOptions GetOptionsInternal()
    {
        var options = CreateOptionsInternal();
        ApplyOptions(options);
        _options = options;
        return options;
    }

    /// <summary>
    /// Creates a new instance of <see cref="JsonSerializerOptions"/>.
    /// </summary>
    private static JsonSerializerOptions CreateOptionsInternal()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(JsonMetadataServices.TimeSpanConverter);
        options.Converters.Add(new IntegerJsonConverter());
        options.Converters.Add(new DecimalJsonConverter());

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