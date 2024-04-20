using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Converters;
using Refit;

namespace Elsa.Api.Client;

/// <summary>
/// Contains helper methods for configuring Refit settings.
/// </summary>
public static class RefitSettingsHelper
{
    private static JsonSerializerOptions? _jsonSerializerOptions;

    /// <summary>
    /// Creates a <see cref="RefitSettings"/> instance configured for Elsa. 
    /// </summary>
    public static RefitSettings CreateRefitSettings()
    {
        var settings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(CreateJsonSerializerOptions())
        };

        return settings;
    }

    /// <summary>
    /// Creates a <see cref="JsonSerializerOptions"/> instance configured for Elsa.
    /// </summary>
    public static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        if (_jsonSerializerOptions != null)
            return _jsonSerializerOptions;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new VersionOptionsJsonConverter());

        return _jsonSerializerOptions = options;
    }
}