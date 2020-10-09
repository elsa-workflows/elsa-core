using System;
using Elsa.Caching;
using Elsa.DistributedLock;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Config;
using Rebus.DataBus.InMem;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.Transport.InMem;
using YesSql;

namespace Elsa
{
    public class ElsaOptions
    {
        public ElsaOptions(IServiceCollection services)
        {
            Services = services;

            SessionFactory = sp => sp.GetRequiredService<ISession>();
            DistributedLockProviderFactory = sp => new DefaultLockProvider();
            SignalFactory = sp => new Signal();
            ServiceBusConfigurer = ConfigureInMemoryServiceBus;
        }

        public IServiceCollection Services { get; }
        internal Func<IServiceProvider, ISession> SessionFactory { get; private set; }
        internal Func<IServiceProvider, IDistributedLockProvider> DistributedLockProviderFactory { get; private set; }
        internal Func<IServiceProvider, ISignal> SignalFactory { get; private set; }
        internal Func<RebusConfigurer, IServiceProvider, RebusConfigurer> ServiceBusConfigurer { get; private set; }

        public ElsaOptions UseDistributedLockProvider(Func<IServiceProvider, IDistributedLockProvider> factory)
        {
            DistributedLockProviderFactory = factory;
            return this;
        }

        public ElsaOptions UseSignal(Func<IServiceProvider, ISignal> factory)
        {
            SignalFactory = factory;
            return this;
        }

        public ElsaOptions ConfigureServiceBus(Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure)
        {
            ServiceBusConfigurer = configure;
            return this;
        }

        public ElsaOptions ConfigureServiceBus(Func<RebusConfigurer, RebusConfigurer> configure) =>
            ConfigureServiceBus((bus, _) => configure(bus));

        private static RebusConfigurer ConfigureInMemoryServiceBus(RebusConfigurer rebus, IServiceProvider serviceProvider)
        {
            return rebus
                .Logging(logging => logging.ColoredConsole())
                .Subscriptions(s => s.StoreInMemory(new InMemorySubscriberStore()))
                .DataBus(s => s.StoreInMemory(new InMemDataStore()))
                .Routing(r => r.TypeBased())
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "Messages"));
        }
    }
}