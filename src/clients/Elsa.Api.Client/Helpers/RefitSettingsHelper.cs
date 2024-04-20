using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Converters;
using Refit;

namespace Elsa.Api.Client;

/// <summary>
/// Contains helper methods for configuring Refit settings.
/// </summary>
public class RefitSettingsHelper
{
    /// <summary>
    /// Creates a <see cref="RefitSettings"/> instance configured for Elsa. 
    /// </summary>
    public static RefitSettings CreateRefitSettings()
    {
        JsonSerializerOptions serializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        serializerOptions.Converters.Add(new JsonStringEnumConverter());
        serializerOptions.Converters.Add(new VersionOptionsJsonConverter());

        var settings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(serializerOptions),
        };

        return settings;
    }
}