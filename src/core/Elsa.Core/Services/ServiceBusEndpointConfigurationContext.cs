using System;
using Rebus.Config;

namespace Elsa.Services
{
    public class ServiceBusEndpointConfigurationContext
    {
        public ServiceBusEndpointConfigurationContext(RebusConfigurer configurer, string queueName, Type messageType, IServiceProvider serviceProvider)
        {
            Configurer = configurer;
            QueueName = queueName;
            MessageType = messageType;
            ServiceProvider = serviceProvider;
        }

        public RebusConfigurer Configurer { get; }
        public string QueueName { get; }
        public Type MessageType { get; }
        public IServiceProvider ServiceProvider { get; set; }
    }
}