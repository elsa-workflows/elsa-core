using System.Text.Json;
using Elsa.Common.Serialization;
using Elsa.Workflows.Options;
using Elsa.Workflows.Serialization.Converters;
using Microsoft.Extensions.Options;

namespace Elsa.Testing.Shared;

/// <summary>
/// Shared arrange helpers for serialization type registry and <see cref="TypeJsonConverter"/> tests.
/// </summary>
public static class SerializationTypeTestHelpers
{
    /// <summary>
    /// Creates a registry from already-configured options.
    /// </summary>
    public static SerializationTypeRegistry CreateRegistry(SerializationTypeOptions options) =>
        new(Options.Create(options));

    /// <summary>
    /// Creates a registry and applies the provided options configuration.
    /// </summary>
    public static SerializationTypeRegistry CreateRegistry(Action<SerializationTypeOptions> configure)
    {
        var options = new SerializationTypeOptions();
        configure(options);
        return CreateRegistry(options);
    }

    /// <summary>
    /// Creates camel-case JSON options that resolve <see cref="Type"/> values through the given registry.
    /// </summary>
    public static JsonSerializerOptions CreateTypeJsonOptions(ISerializationTypeRegistry registry) => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new TypeJsonConverter(registry) }
    };
}
