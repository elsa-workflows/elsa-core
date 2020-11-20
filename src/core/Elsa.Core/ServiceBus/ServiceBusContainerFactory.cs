using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Config;

namespace Elsa.ServiceBus
{
    public class ServiceBusContainerFactory : IServiceBusContainerFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDictionary<string, IServiceBusContainer> _containers = new Dictionary<string, IServiceBusContainer>();

        public ServiceBusContainerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        public IServiceBusContainer CreateBusContainer(string name, Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure)
        {
            var container = new ServiceBusContainer(name, _serviceProvider, configure);
            _containers.Add(name, container);
            return container;
        }

        public IServiceBusContainer GetBusContainer(string name) => _containers[name];
    }
}