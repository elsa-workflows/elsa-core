using System.Text.Json;
using Elsa.Common.Serialization;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.Workflows.Serialization.Serializers;

/// <inheritdoc cref="IApiSerializer" />
public class ApiSerializer : ConfigurableSerializer, IApiSerializer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiSerializer"/> class.
    /// </summary>
    public ApiSerializer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    /// <inheritdoc />
    public string Serialize(object model)
    {
        var options = GetOptions();
        return JsonSerializer.Serialize(model, options);
    }

    /// <inheritdoc />
    public object Deserialize(string serializedModel) => Deserialize<object>(serializedModel);

    /// <inheritdoc />
    public T Deserialize<T>(string serializedModel)
    {
        var options = GetOptions();
        return JsonSerializer.Deserialize<T>(serializedModel, options)!;
    }

    /// <inheritdoc />
    protected override void Configure(JsonSerializerOptions options)
    {
        options.PropertyNameCaseInsensitive = true;
    }

    /// <inheritdoc />
    protected override void AddConverters(JsonSerializerOptions options)
    {
        options.Converters.Add(CreateInstance<TypeJsonConverter>());
    }

    JsonSerializerOptions IApiSerializer.GetOptions() => GetOptions();

    JsonSerializerOptions IApiSerializer.ApplyOptions(JsonSerializerOptions options)
    {
        ApplyOptions(options);
        return options;
    }
}