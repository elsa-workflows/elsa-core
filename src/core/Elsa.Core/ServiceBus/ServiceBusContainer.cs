using System;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus;
using Rebus.Config;
using Rebus.ServiceProvider;

namespace Elsa.ServiceBus
{
    public class ServiceBusContainer : IServiceBusContainer, IDisposable
    {
        public ServiceBusContainer(string name, IServiceProvider serviceProvider, Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure)
        {
            Name = name;

            var configurer = Configure.With(new DependencyInjectionHandlerActivator(serviceProvider));
            configurer = configure(configurer, serviceProvider);
            Bus = configurer.Start();
        }
        
        public string Name { get; }
        public IBus Bus { get; }

        public void Dispose() => Bus.Dispose();
    }
}