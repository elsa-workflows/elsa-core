using System;
using System.Collections.Generic;
using Rebus.Bus;
using Rebus.Config;
using Rebus.ServiceProvider;

namespace Elsa.Services
{
    public class ServiceBusFactory : IServiceBusFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDictionary<string, IBus> _serviceBuses = new Dictionary<string, IBus>();

        public ServiceBusFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        public IBus CreateServiceBus(string name, Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure)
        {
            var configurer = Configure.With(new DependencyInjectionHandlerActivator(_serviceProvider));
            configurer = configure(configurer, _serviceProvider);

            var bus = configurer.Start();
            _serviceBuses.Add(name, bus);
            
            return bus;
        }

        public IBus GetServiceBus(string name) => _serviceBuses[name];
    }
}