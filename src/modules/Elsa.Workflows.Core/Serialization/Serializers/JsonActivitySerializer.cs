using System.Text.Json;
using Elsa.Common.Serialization;
using Elsa.Extensions;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.Workflows.Serialization.Serializers;

/// <inheritdoc cref="IActivitySerializer" />
public class JsonActivitySerializer(IServiceProvider serviceProvider) : ConfigurableSerializer(serviceProvider), IActivitySerializer
{
    private JsonSerializerOptions? _options;
    
    /// <inheritdoc />
    public string Serialize(IActivity activity)
    {
        var options = GetOptionsInternal();
        return JsonSerializer.Serialize(activity, activity.GetType(), options);
    }

    /// <inheritdoc />
    public string Serialize(object value)
    {
        var options = GetOptionsInternal();
        return JsonSerializer.Serialize(value, options);
    }

    /// <inheritdoc />
    public IActivity Deserialize(string serializedActivity) => JsonSerializer.Deserialize<IActivity>(serializedActivity, GetOptions())!;

    /// <inheritdoc />
    public object Deserialize(string serializedValue, Type type) => JsonSerializer.Deserialize(serializedValue, type, GetOptions())!;

    /// <inheritdoc />
    public T Deserialize<T>(string serializedValue) => JsonSerializer.Deserialize<T>(serializedValue, GetOptions())!;

    /// <inheritdoc />
    protected override void AddConverters(JsonSerializerOptions options)
    {
        options.Converters.Add(CreateInstance<TypeJsonConverter>());
        options.Converters.Add(CreateInstance<InputJsonConverterFactory>());
    }
    
    private JsonSerializerOptions GetOptionsInternal()
    {
        if(_options != null)
            return _options;
        
        var options = GetOptions().Clone();
        options.Converters.Add(CreateInstance<JsonIgnoreCompositeRootConverterFactory>());
        return _options = options;
    }
}