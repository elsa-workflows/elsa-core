using Elsa.Services;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Routing.TypeBased;

namespace Elsa.Rebus.RabbitMq.Extensions
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaOptions UseRabbitMq(this ElsaOptions elsaOptions, string connectionString) => elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString));
        private static void ConfigureAzureServiceBusEndpoint(ServiceBusEndpointConfigurationContext context, string connectionString) => context.Configurer.Transport(t => t.UseRabbitMq(connectionString, context.QueueName));
    }
}