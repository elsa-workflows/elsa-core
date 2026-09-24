using System.Text;
using System.Text.Json;
using Elsa.Common.Serialization;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.Serialization.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Serialization.Serializers;

/// <inheritdoc cref="IActivitySerializer" />
public class JsonActivitySerializer(IServiceProvider serviceProvider) : ConfigurableSerializer(serviceProvider), IActivitySerializer
{
    /// <inheritdoc />
    public string Serialize(IActivity activity)
    {
        var options = GetOptionsInternal();
        var descriptor = ServiceProvider.GetRequiredService<IActivityRegistry>().Find(activity.Type, activity.Version);
        if (descriptor is null || !descriptor.Inputs.Any(input => input.IsSynthetic) && !descriptor.Outputs.Any(output => output.IsSynthetic))
        {
            return JsonSerializer.Serialize(activity, activity.GetType(), options);
        }

        // Keep normal CLR properties on System.Text.Json's metadata-aware path.
        // Only synthetic descriptor properties need the activity-specific writer.
        options = descriptor.ConfigureSerializerOptions?.Invoke(options) ?? options;
        var serialized = JsonSerializer.SerializeToElement(activity, activity.GetType(), options);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = options.WriteIndented, Encoder = options.Encoder }))
        {
            writer.WriteStartObject();
            foreach (var property in serialized.EnumerateObject())
            {
                property.WriteTo(writer);
            }
            ServiceProvider.GetRequiredService<SyntheticPropertiesWriter>().WriteSyntheticProperties(writer, activity, descriptor, options);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <inheritdoc />
    public string Serialize(object value)
    {
        if (value is IActivity activity)
        {
            return Serialize(activity);
        }

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
        options.Converters.Add(CreateInstance<OutputJsonConverterFactory>());
        options.Converters.Add(CreateInstance<ExpressionJsonConverterFactory>());
        options.Converters.Add(CreateInstance<FuncExpressionValueConverter>());
    }
}
