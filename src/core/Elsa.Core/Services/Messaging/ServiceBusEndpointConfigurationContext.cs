using System;
using System.Collections.Generic;
using Rebus.Config;

namespace Elsa.Services.Messaging
{
    public class ServiceBusEndpointConfigurationContext
    {
        public ServiceBusEndpointConfigurationContext(RebusConfigurer configurer, string queueName, IDictionary<Type, string> messageTypeMap, IServiceProvider serviceProvider, bool autoDeleteOnIdle)
        {
            Configurer = configurer;
            QueueName = queueName;
            MessageTypeMap = messageTypeMap;
            ServiceProvider = serviceProvider;
            AutoDeleteOnIdle = autoDeleteOnIdle;
        }

        public RebusConfigurer Configurer { get; }
        public string QueueName { get; }
        public IDictionary<Type, string> MessageTypeMap { get; }
        public IServiceProvider ServiceProvider { get; set; }
        public bool AutoDeleteOnIdle { get; }
    }
}