using Elsa.ServiceBus.MassTransit.Extensions;
using Elsa.ServiceBus.MassTransit.Options;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MassTransit.Extensions;

/// <summary>
/// Extension methods for configuring Azure Service Bus with MassTransit in Elsa with support for IBusRegistrationContext.
/// </summary>
public static class AzureServiceBusExtensions
{
    /// <summary>
    /// Configures Azure Service Bus as the transport for MassTransit with support for IBusRegistrationContext.
    /// </summary>
    /// <param name="options">The MassTransit options.</param>
    /// <param name="connectionString">The Azure Service Bus connection string.</param>
    /// <param name="configureServiceBus">An optional delegate to configure the Azure Service Bus transport.</param>
    /// <returns>The MassTransit options.</returns>
    public static MassTransitServiceBusOptions UseAzureServiceBusWithContext(
        this MassTransitServiceBusOptions options,
        string connectionString, 
        Action<AzureServiceBusOptions>? configureServiceBus = default)
    {
        return options.UseAzureServiceBus(connectionString, asbOptions =>
        {
            // Copy any configuration from the provided delegate
            configureServiceBus?.Invoke(asbOptions);
            
            // Wrap the original ConfigureTransportBus delegate if it exists
            var originalConfigureTransportBus = asbOptions.ConfigureTransportBus;
            
            // Replace with a new delegate that forwards the IBusRegistrationContext
            asbOptions.ConfigureTransportBus = (context, configurator) =>
            {
                // Call the original delegate if it exists
                originalConfigureTransportBus?.Invoke(context, configurator);
                
                // Get the scoped filter registration delegate if it was set
                var configureWithContext = asbOptions.GetConfigureWithContext();
                
                // Invoke the delegate that requires IBusRegistrationContext
                configureWithContext?.Invoke(configurator, context);
            };
        });
    }
    
    /// <summary>
    /// Sets a configuration delegate that requires IBusRegistrationContext for configuring the transport.
    /// </summary>
    /// <param name="options">The Azure Service Bus options.</param>
    /// <param name="configure">A delegate to configure the Azure Service Bus transport with access to IBusRegistrationContext.</param>
    /// <returns>The Azure Service Bus options.</returns>
    public static AzureServiceBusOptions ConfigureUsingContext(
        this AzureServiceBusOptions options,
        Action<IServiceBusBusFactoryConfigurator, IBusRegistrationContext> configure)
    {
        options.SetConfigureWithContext(configure);
        return options;
    }
    
    // Extension methods for storing/retrieving the delegate in a consistent way
    private static readonly string ConfigureWithContextKey = "ConfigureWithContext";
    
    private static void SetConfigureWithContext(
        this AzureServiceBusOptions options, 
        Action<IServiceBusBusFactoryConfigurator, IBusRegistrationContext> configure)
    {
        options.Properties[ConfigureWithContextKey] = configure;
    }
    
    private static Action<IServiceBusBusFactoryConfigurator, IBusRegistrationContext>? GetConfigureWithContext(this AzureServiceBusOptions options)
    {
        return options.Properties.TryGetValue(ConfigureWithContextKey, out var value) && 
               value is Action<IServiceBusBusFactoryConfigurator, IBusRegistrationContext> configure 
            ? configure 
            : null;
    }
}