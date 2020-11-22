using System;
using System.Collections.Generic;
using Rebus.Bus;
using Rebus.Config;
using Rebus.ServiceProvider;

namespace Elsa.Services
{
    public class ServiceBusFactory : IServiceBusFactory
    {
        private readonly ElsaOptions _elsaOptions;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDictionary<string, IBus> _serviceBuses = new Dictionary<string, IBus>();
        private readonly DependencyInjectionHandlerActivator _handlerActivator;

        public ServiceBusFactory(ElsaOptions elsaOptions, IServiceProvider serviceProvider)
        {
            _elsaOptions = elsaOptions;
            _serviceProvider = serviceProvider;
            _handlerActivator = new DependencyInjectionHandlerActivator(serviceProvider);
        }

        public IBus GetServiceBus(Type messageType)
        {
            var queueName = messageType.Name;

            if (_serviceBuses.TryGetValue(queueName, out var bus))
                return bus;

            var configurer = Configure.With(_handlerActivator);
            var map = new Dictionary<Type, string> { [messageType] = queueName };
            var configureContext = new ServiceBusEndpointConfigurationContext(configurer, queueName, map, _serviceProvider);

            _elsaOptions.ConfigureServiceBusEndpoint(configureContext);

            var newBus = configurer.Start();
            _serviceBuses.Add(queueName, newBus);

            return newBus;
        }
    }
}