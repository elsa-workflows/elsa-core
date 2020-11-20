using System;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus;

namespace Elsa.ServiceBus
{
    public class ServiceBusContainer : IServiceBusContainer, IDisposable
    {
        public ServiceBusContainer(Action<IServiceCollection> configure)
        {
            var services = new ServiceCollection();
            configure(services);
            ServiceProvider = services.BuildServiceProvider();
            Bus = ServiceProvider.GetRequiredService<IBus>();
        }
        
        public IServiceProvider ServiceProvider { get; }
        public IBus Bus { get; }

        public void Dispose() => ((IDisposable)ServiceProvider).Dispose();
    }
}