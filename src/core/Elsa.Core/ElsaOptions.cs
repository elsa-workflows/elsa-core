using System;
using Elsa.Caching;
using Elsa.DistributedLock;
using Elsa.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rebus.Config;
using Rebus.DataBus.InMem;
using Rebus.Logging;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.Transport.InMem;
using YesSql;
using YesSql.Provider.Sqlite.InMemory;

namespace Elsa
{
    public class ElsaOptions
    {
        public ElsaOptions(IServiceCollection services)
        {
            Services = services;

            ConfigurePersistence = (sp, config) => config.UseInMemorySqlite();
            DistributedLockProviderFactory = sp => new DefaultLockProvider();
            SignalFactory = sp => new Signal();
            ServiceBusConfigurer = ConfigureInMemoryServiceBus;
            JsonSerializerConfigurer = (sp, serializer) => {};
            
            CreateJsonSerializer = sp =>
            {
                var serializer = DefaultJsonSerializer.CreateDefaultJsonSerializer();
                JsonSerializerConfigurer(sp, serializer);
                return serializer;
            };
        }

        public IServiceCollection Services { get; }
        internal Action<IServiceProvider, IConfiguration> ConfigurePersistence { get; set; }
        internal Func<IServiceProvider, IDistributedLockProvider> DistributedLockProviderFactory { get; private set; }
        internal Func<IServiceProvider, ISignal> SignalFactory { get; private set; }
        internal Func<RebusConfigurer, IServiceProvider, RebusConfigurer> ServiceBusConfigurer { get; private set; }
        internal Func<IServiceProvider, JsonSerializer> CreateJsonSerializer { get; private set; }
        internal Action<IServiceProvider, JsonSerializer> JsonSerializerConfigurer { get; private set; }

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

        public ElsaOptions UsePersistence(Action<IConfiguration> configure) => UsePersistence((sp, config) => configure(config));

        public ElsaOptions UsePersistence(Action<IServiceProvider, IConfiguration> configure)
        {
            ConfigurePersistence = configure;
            return this;
        }

        public ElsaOptions ConfigureServiceBus(Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure)
        {
            ServiceBusConfigurer = configure;
            return this;
        }

        public ElsaOptions ConfigureServiceBus(Func<RebusConfigurer, RebusConfigurer> configure) => ConfigureServiceBus((bus, _) => configure(bus));

        public ElsaOptions UseJsonSerializer(Func<IServiceProvider, JsonSerializer> factory)
        {
            CreateJsonSerializer = factory;
            return this;
        }

        public ElsaOptions ConfigureJsonSerializer(Action<IServiceProvider, JsonSerializer> configure)
        {
            JsonSerializerConfigurer = configure;
            return this;
        }

        private static RebusConfigurer ConfigureInMemoryServiceBus(RebusConfigurer rebus,
            IServiceProvider serviceProvider)
        {
            return rebus
                .Logging(logging => logging.ColoredConsole(LogLevel.Info))
                .Subscriptions(s => s.StoreInMemory(new InMemorySubscriberStore()))
                .DataBus(s => s.StoreInMemory(new InMemDataStore()))
                .Routing(r => r.TypeBased())
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "Messages"));
        }
    }
}