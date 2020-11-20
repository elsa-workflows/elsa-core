using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceBus
{
    public class ServiceBusContainerFactory : IServiceBusContainerFactory 
    {
        private readonly ICollection<IServiceBusContainer> _containers = new List<IServiceBusContainer>();

        public IServiceBusContainer CreateBusContainer(Action<IServiceCollection> configure)
        {
            var container = new ServiceBusContainer(configure);
            _containers.Add(container);
            return container;
        }
    }
}