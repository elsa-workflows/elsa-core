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
    /// <summary>
    /// Creates a <see cref="RefitSettings"/> instance configured for Elsa. 
    /// </summary>
    public static RefitSettings CreateRefitSettings(IServiceProvider serviceProvider, Action<IServiceProvider, JsonSerializerOptions>? configureJsonSerializerOptions = null)
    {
        var settings = new RefitSettings { ContentSerializer = new SystemTextJsonContentSerializer(CreateJsonSerializerOptions(serviceProvider, configureJsonSerializerOptions)) };

        return settings;
    }

    /// <summary>
    /// Creates a <see cref="JsonSerializerOptions"/> instance configured for Elsa.
    /// </summary>
    public static JsonSerializerOptions CreateJsonSerializerOptions(IServiceProvider serviceProvider, Action<IServiceProvider, JsonSerializerOptions>? configureJsonSerializerOptions = null)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new VersionOptionsJsonConverter());
        options.Converters.Add(new TypeJsonConverter());
        
        configureJsonSerializerOptions?.Invoke(serviceProvider, options);
        
        return options;
    }
}