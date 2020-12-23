using Elsa.Services;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Routing.TypeBased;

namespace Elsa.Rebus.RabbitMq.Extensions
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaOptions UseRabbitMq(this ElsaOptions elsaOptions, string connectionString, LogLevel logLevel = LogLevel.Info)
        {
            return elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString, logLevel));
        } 
        
        private static void ConfigureAzureServiceBusEndpoint(ServiceBusEndpointConfigurationContext context, string connectionString, LogLevel logLevel)
        {
            var queueName = context.QueueName;

            context.Configurer
                .Logging(l => l.ColoredConsole(logLevel))
                .Transport(t => t.UseRabbitMq(connectionString, queueName))
                .Routing(r => r.TypeBased().Map(context.MessageTypeMap));
        }
    }
}