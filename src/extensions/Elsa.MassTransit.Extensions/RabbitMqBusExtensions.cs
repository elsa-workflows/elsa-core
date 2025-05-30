using Elsa.ServiceBus.MassTransit.Extensions;
using Elsa.ServiceBus.MassTransit.Options;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MassTransit.Extensions;

/// <summary>
/// Extension methods for configuring RabbitMQ with MassTransit in Elsa with support for IBusRegistrationContext.
/// </summary>
public static class RabbitMqBusExtensions
{
    /// <summary>
    /// Configures RabbitMQ as the transport for MassTransit with support for IBusRegistrationContext.
    /// </summary>
    /// <param name="options">The MassTransit options.</param>
    /// <param name="connectionString">The RabbitMQ connection string.</param>
    /// <param name="configureRabbit">An optional delegate to configure the RabbitMQ transport.</param>
    /// <returns>The MassTransit options.</returns>
    public static MassTransitServiceBusOptions UseRabbitMqWithContext(
        this MassTransitServiceBusOptions options,
        string connectionString,
        Action<RabbitMqOptions>? configureRabbit = default)
    {
        return options.UseRabbitMq(connectionString, rabbitOptions =>
        {
            // Copy any configuration from the provided delegate
            configureRabbit?.Invoke(rabbitOptions);
            
            // Wrap the original ConfigureTransportBus delegate if it exists
            var originalConfigureTransportBus = rabbitOptions.ConfigureTransportBus;
            
            // Replace with a new delegate that forwards the IBusRegistrationContext
            rabbitOptions.ConfigureTransportBus = (context, configurator) =>
            {
                // Call the original delegate if it exists
                originalConfigureTransportBus?.Invoke(context, configurator);
                
                // Get the scoped filter registration delegate if it was set
                var configureWithContext = rabbitOptions.GetConfigureWithContext();
                
                // Invoke the delegate that requires IBusRegistrationContext
                configureWithContext?.Invoke(configurator, context);
            };
        });
    }
    
    /// <summary>
    /// Sets a configuration delegate that requires IBusRegistrationContext for configuring the transport.
    /// </summary>
    /// <param name="options">The RabbitMQ options.</param>
    /// <param name="configure">A delegate to configure the RabbitMQ transport with access to IBusRegistrationContext.</param>
    /// <returns>The RabbitMQ options.</returns>
    public static RabbitMqOptions ConfigureUsingContext(
        this RabbitMqOptions options,
        Action<IRabbitMqBusFactoryConfigurator, IBusRegistrationContext> configure)
    {
        options.SetConfigureWithContext(configure);
        return options;
    }
    
    // Extension methods for storing/retrieving the delegate in a consistent way
    private static readonly string ConfigureWithContextKey = "ConfigureWithContext";
    
    private static void SetConfigureWithContext(
        this RabbitMqOptions options, 
        Action<IRabbitMqBusFactoryConfigurator, IBusRegistrationContext> configure)
    {
        options.Properties[ConfigureWithContextKey] = configure;
    }
    
    private static Action<IRabbitMqBusFactoryConfigurator, IBusRegistrationContext>? GetConfigureWithContext(this RabbitMqOptions options)
    {
        return options.Properties.TryGetValue(ConfigureWithContextKey, out var value) && 
               value is Action<IRabbitMqBusFactoryConfigurator, IBusRegistrationContext> configure 
            ? configure 
            : null;
    }
}