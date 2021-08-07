using System;
using Rebus.Config;

namespace Elsa.Services.Messaging
{
    public class ServiceBusPublishEndpointConfigurationContext
    {
        public ServiceBusPublishEndpointConfigurationContext(RebusConfigurer configurer, string queueName, IServiceProvider serviceProvider)
        {
            Configurer = configurer;
            QueueName = queueName;
            ServiceProvider = serviceProvider;
        }

        public RebusConfigurer Configurer { get; }
        public string QueueName { get; }
        public IServiceProvider ServiceProvider { get; set; }
    }
}