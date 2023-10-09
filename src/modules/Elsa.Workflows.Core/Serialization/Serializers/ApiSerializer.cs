using System.Text.Json;
using Elsa.Common.Serialization;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;

namespace Elsa.Workflows.Core.Serialization.Serializers;

/// <inheritdoc cref="Elsa.Workflows.Core.Contracts.IApiSerializer" />
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
        var options = CreateOptions();
        return JsonSerializer.Serialize(model, options);
    }

    /// <inheritdoc />
    public object Deserialize(string serializedModel) => Deserialize<object>(serializedModel);

    /// <inheritdoc />
    public T Deserialize<T>(string serializedModel)
    {
        var options = CreateOptions();
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

    JsonSerializerOptions IApiSerializer.CreateOptions() => base.CreateOptions();

    JsonSerializerOptions IApiSerializer.ApplyOptions(JsonSerializerOptions options)
    {
        Apply(options);
        return options;
    }
}