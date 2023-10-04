using System.Text.Json;
using Elsa.Common.Serialization;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;

namespace Elsa.Workflows.Core.Serialization.Serializers;

/// <inheritdoc cref="Elsa.Workflows.Core.Contracts.IActivitySerializer" />
public class JsonActivitySerializer : ConfigurableSerializer, IActivitySerializer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonActivitySerializer"/> class.
    /// </summary>
    public JsonActivitySerializer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    /// <inheritdoc />
    public string Serialize(IActivity activity)
    {
        var options = CreateOptions();
        options.Converters.Add(CreateInstance<JsonIgnoreCompositeRootConverterFactory>());
        return JsonSerializer.Serialize(activity, activity.GetType(), options);
    }

    /// <inheritdoc />
    public string Serialize(object value)
    {
        var options = CreateOptions();
        options.Converters.Add(CreateInstance<JsonIgnoreCompositeRootConverterFactory>());
        return JsonSerializer.Serialize(value, options);
    }

    /// <inheritdoc />
    public IActivity Deserialize(string serializedActivity) => JsonSerializer.Deserialize<IActivity>(serializedActivity, CreateOptions())!;

    /// <inheritdoc />
    public object Deserialize(string serializedValue, Type type) => JsonSerializer.Deserialize(serializedValue, type, CreateOptions())!;

    /// <inheritdoc />
    public T Deserialize<T>(string serializedValue) => JsonSerializer.Deserialize<T>(serializedValue, CreateOptions())!;

    /// <inheritdoc />
    protected override void AddConverters(JsonSerializerOptions options)
    {
        options.Converters.Add(CreateInstance<TypeJsonConverter>());
    }
}