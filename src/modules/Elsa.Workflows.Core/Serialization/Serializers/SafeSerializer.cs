using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Common.Serialization;
using Elsa.Expressions.Services;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.Workflows.Serialization.Serializers;

/// <inheritdoc cref="ISafeSerializer" />
public class SafeSerializer : ConfigurableSerializer, ISafeSerializer
{
    /// <inheritdoc />
    public SafeSerializer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type T may be trimmed.")]
    public ValueTask<string> SerializeAsync(object? value, CancellationToken cancellationToken = default)
    {
        var options = GetOptions();
        return ValueTask.FromResult(JsonSerializer.Serialize(value, options));
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type T may be trimmed.")]
    public ValueTask<JsonElement> SerializeToElementAsync(object? value, CancellationToken cancellationToken = default)
    {
        var options = GetOptions();
        return new(JsonSerializer.SerializeToElement(value, options));
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type T may be trimmed.")]
    public ValueTask<T> DeserializeAsync<T>(string json, CancellationToken cancellationToken = default)
    {
        var options = GetOptions();
        return new(JsonSerializer.Deserialize<T>(json, options)!);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type T may be trimmed.")]
    public ValueTask<T> DeserializeAsync<T>(JsonElement element, CancellationToken cancellationToken = default)
    {
        var options = GetOptions();
        return new(element.Deserialize<T>(options)!);
    }

    /// <inheritdoc />
    protected override void AddConverters(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new TypeJsonConverter(WellKnownTypeRegistry.CreateDefault()));
        options.Converters.Add(new SafeValueConverterFactory());
    }
}