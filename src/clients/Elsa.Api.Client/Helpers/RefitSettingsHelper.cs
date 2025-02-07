using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.Resources.Alterations.Contracts;
using Elsa.Api.Client.Resources.Alterations.Models;
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

        var alterationTypes = new[] { typeof(Cancel) };

        options.TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            .WithAddedModifier(typeInfo =>
            {
                if (typeInfo.Type != typeof(IAlteration))
                    return;

                if (typeInfo.Kind != JsonTypeInfoKind.Object)
                    return;

                var polymorphismOptions = new JsonPolymorphismOptions { TypeDiscriminatorPropertyName = "type" };

                foreach (var alterationType in alterationTypes.ToList())
                {
                    polymorphismOptions.DerivedTypes.Add(new(alterationType, alterationType.Name));
                }

                typeInfo.PolymorphismOptions = polymorphismOptions;
            });

        configureJsonSerializerOptions?.Invoke(serviceProvider, options);

        return options;
    }
}