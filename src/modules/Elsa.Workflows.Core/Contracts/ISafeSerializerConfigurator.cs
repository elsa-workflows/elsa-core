using System.Text.Json;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Provides custom serialization for a given value.
/// Use this when you need to serialize/deserialize a value that is not supported by the default serializer, such as file streams that you want to serialize as a URL.
/// </summary>
public interface ISafeSerializerConfigurator
{
    /// <summary>
    /// Configures the options for the serializer.
    /// </summary>
    void ConfigureOptions(JsonSerializerOptions options);
}