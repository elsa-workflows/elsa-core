using Elsa.Services;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using LogLevel = Rebus.Logging.LogLevel;

namespace Elsa.Rebus.AzureServiceBus
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaOptions UseAzureServiceBus(this ElsaOptions elsaOptions, string connectionString, ITokenProvider? tokenProvider = default)
        {
            return elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString, tokenProvider));
        } 
        
        private static void ConfigureAzureServiceBusEndpoint(ServiceBusEndpointConfigurationContext context, string connectionString, ITokenProvider? tokenProvider)
        {
            var queueName = context.QueueName;
            var loggerFactory = context.ServiceProvider.GetRequiredService<ILoggerFactory>();

            context.Configurer
                .Logging(l => l.MicrosoftExtensionsLogging(loggerFactory))
                .Transport(t => t.UseAzureServiceBus(connectionString, queueName, tokenProvider))
                .Routing(r => r.TypeBased().Map(context.MessageTypeMap));
        }
    }
}