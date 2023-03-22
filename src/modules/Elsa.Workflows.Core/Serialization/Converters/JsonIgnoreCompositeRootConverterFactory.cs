using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// A <see cref="JsonConverterFactory"/> that creates <see cref="JsonIgnoreCompositeRootConverter"/> instances.
/// </summary>
public class JsonIgnoreCompositeRootConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeof(IActivity).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return new JsonIgnoreCompositeRootConverter();
    }
}