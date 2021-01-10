using Elsa.Services;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rebus.Config;
using Rebus.Routing.TypeBased;

namespace Elsa.Rebus.AzureServiceBus
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaOptions UseAzureServiceBus(this ElsaOptions elsaOptions, string connectionString, ITokenProvider? tokenProvider = default)
        {
            return elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(elsaOptions, context, connectionString, tokenProvider));
        } 
        
        private static void ConfigureAzureServiceBusEndpoint(ElsaOptions elsaOptions, ServiceBusEndpointConfigurationContext context, string connectionString, ITokenProvider? tokenProvider)
        {
            var queueName = context.QueueName;
            var loggerFactory = context.ServiceProvider.GetRequiredService<ILoggerFactory>();

            context.Configurer
                .Logging(l => l.MicrosoftExtensionsLogging(loggerFactory))
                .Transport(t => t.UseAzureServiceBus(connectionString, queueName, tokenProvider))
                .Routing(r => r.TypeBased().Map(context.MessageTypeMap))
                .Options(o => o.Apply(elsaOptions.ServiceBusOptions));
        }
    }
}