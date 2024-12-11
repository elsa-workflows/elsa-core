using Elsa.Common.Serialization;
using Elsa.Extensions;
using Elsa.Kafka;
using Elsa.Workflows.LogPersistence.Strategies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trimble.Elsa.Activities.Activities.Expressions;
using Trimble.Elsa.Activities.Kafka;

namespace Trimble.Elsa.Activities.Config;

public static class ConfigurationExtensions
{
    internal static bool ActivityHostLoggingEnabled { get; set; }

    /// <summary>
    /// Loads Elsa-relevant authentication credentials from the TokenProviders section 
    /// of the config file or environment variables.
    /// <param name="sectionName">Typically "TokenProviders"</param>
    /// </summary>
    public static IServiceCollection AddActivityTokenProviders(this WebApplicationBuilder webAppBuilder, string sectionName)
    {
        return webAppBuilder.Services.AddActivityTokenProviders(webAppBuilder.Configuration, sectionName);
    }

    /// <summary>
    /// Loads Elsa-relevant authentication credentials from the TokenProviders section 
    /// of the config file or environment variables.
    /// <param name="sectionName">Typically "TokenProviders"</param>
    /// </summary>
    public static IServiceCollection AddActivityTokenProviders(this IServiceCollection services, IConfiguration config, string sectionName)
    {
        AuthnTokenProviders? authnTokenProviders = config.GetSection(sectionName).Get<AuthnTokenProviders>();

        if (authnTokenProviders is null)
        {
            // This will fault if used in a workflow without the necessary values
            authnTokenProviders = new AuthnTokenProviders()
            {
                SalesforceDX = new SalesforceDXTokenProvider(),
                TrimbleId = new TrimbleIdTokenProvider(),
                ViewpointId = new ViewpointIdTokenProvider()
            };
        }
        services.AddSingleton(authnTokenProviders.ViewpointId)
            .AddSingleton(authnTokenProviders.TrimbleId)
            .AddSingleton(authnTokenProviders.SalesforceDX);

        return services;
    }

    /// <summary>
    /// Adds the custom Elsa expression handlers. 
    /// Necessary because script activities break due to presence of reserved characters 
    /// that break C#, JavaScript language syntax.
    /// </summary>
    public static IServiceCollection AddCustomExpressions(this WebApplicationBuilder webAppBuilder)
    {
        return AddCustomExpressions(webAppBuilder.Services);
    }

    /// <summary>
    /// Adds the custom Elsa expression handlers. 
    /// Necessary because script activities break due to presence of reserved characters 
    /// that break C#, JavaScript language syntax.
    /// </summary>
    public static IServiceCollection AddCustomExpressions(this IServiceCollection services)
    {
        return services.AddExpressionDescriptorProvider<RegexVariableExpressionDescriptorProvider>();
    }

    public static WebApplicationBuilder ConfigureSources(this KafkaOptions kafkaOptions, WebApplicationBuilder webAppBuilder)
    {
        webAppBuilder.Configuration.GetSection("Kafka").Bind(kafkaOptions);

        foreach (var schemaRegistry in kafkaOptions.SchemaRegistries)
        {
            var apiKeyEnv = Environment.GetEnvironmentVariable($"KAFKA_SCHEMAREG_{schemaRegistry.Name.ToUpper().Replace(" ", "_")}_KEY");
            var apiSecretEnv = Environment.GetEnvironmentVariable($"KAFKA_SCHEMAREG_{schemaRegistry.Name.ToUpper().Replace(" ", "_")}_SECRET");


            if (!string.IsNullOrEmpty(apiKeyEnv) && !string.IsNullOrEmpty(apiSecretEnv))
            {
                schemaRegistry.Config.BasicAuthUserInfo = $"{apiKeyEnv}:{apiSecretEnv}";
            }
        }

        return webAppBuilder;
    }

    public static IServiceCollection ConfigureKafka(this IServiceCollection services)
    {
        TypeAliasRegistry.RegisterAlias("AvroProducerFactory", typeof(AvroProducerFactory<string, AvroDataPropertyMessage>));
        TypeAliasRegistry.RegisterAlias("AvroConsumerFactory", typeof(AvroConsumerFactory<string, AvroDataPropertyMessage>));

        return services;
    }

    /// <summary>
    /// Toggles the ability to format and forward custom Elsa Activity messages to the configured Microsoft.Extensions.Logging provider
    /// </summary>
    public static IServiceCollection SetActivityHostLogging(this IServiceCollection services, bool enabled)
    {
        ActivityHostLoggingEnabled = enabled;

        return services;
    }

    /// <summary>
    /// Bootstrap individual registration methods
    /// </summary>
    public static IServiceCollection UseTrimbleActivities(this IServiceCollection services, IConfiguration config, string sectionName)
    {
        services
            .AddActivityTokenProviders(config, sectionName)
            .AddCustomExpressions();

        return services;
    }
}