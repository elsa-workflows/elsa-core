using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Services;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Azure.Services.AppAuthentication;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Routing.TypeBased;

namespace Elsa.ServiceBus.AzureServiceBus
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaOptions UseAzureServiceBus(this ElsaOptions elsaOptions, string connectionString, LogLevel logLevel = LogLevel.Info, ITokenProvider? tokenProvider = default)
        {
            return elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString, logLevel, tokenProvider));
        } 
        
        private static void ConfigureAzureServiceBusEndpoint(ServiceBusEndpointConfigurationContext context, string connectionString, LogLevel logLevel, ITokenProvider? tokenProvider)
        {
            var queueName = context.QueueName;

            context.Configurer
                .Logging(l => l.ColoredConsole(logLevel))
                .Transport(t => t.UseAzureServiceBus(connectionString, queueName, tokenProvider))
                .Routing(r => r.TypeBased().Map(context.MessageTypeMap));
        }
    }
}