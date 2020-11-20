using System;
using System.Data;
using Elsa.Caching;
using Elsa.DistributedLock;
using Elsa.Extensions;
using Elsa.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rebus.Config;
using Rebus.DataBus.InMem;
using Rebus.Logging;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.ServiceProvider;
using Rebus.Transport.InMem;
using YesSql;
using Storage.Net;
using Storage.Net.Blobs;
using YesSql.Provider.Sqlite;

namespace Elsa
{
    public class ElsaOptions
    {
        public ElsaOptions(IServiceCollection services)
        {
            Services = services;

            ConfigurePersistence = (sp, config) => config.UseSqLite("Data Source=elsa.db;Cache=Shared", IsolationLevel.ReadUncommitted);
            StorageFactory = sp => Storage.Net.StorageFactory.Blobs.InMemory();
            DistributedLockProviderFactory = sp => new DefaultLockProvider();
            SignalFactory = sp => new Signal();
            JsonSerializerConfigurer = (sp, serializer) => {};
            
            AddAutoMapper = () =>
            {
                services.AddAutoMapper(ServiceLifetime.Singleton);
                services.AddSingleton(sp => sp.CreateAutoMapperConfiguration());
            };

            AddServiceBus = () =>
            {
                services.AddSingleton<InMemNetwork>();
                services.AddSingleton<InMemorySubscriberStore>();
                services.AddSingleton<InMemDataStore>();
                services.AddRebus(ConfigureInMemoryServiceBus);
            };
            
            CreateJsonSerializer = sp =>
            {
                var serializer = DefaultContentSerializer.CreateDefaultJsonSerializer();
                JsonSerializerConfigurer(sp, serializer);
                return serializer;
            };
        }

        public IServiceCollection Services { get; }
        internal Action<IServiceProvider, IConfiguration> ConfigurePersistence { get; set; }
        internal Func<IServiceProvider, IBlobStorage> StorageFactory { get; set; }
        internal Func<IServiceProvider, IDistributedLockProvider> DistributedLockProviderFactory { get; private set; }
        internal Func<IServiceProvider, ISignal> SignalFactory { get; private set; }
        internal Func<IServiceProvider, JsonSerializer> CreateJsonSerializer { get; private set; }
        internal Action<IServiceProvider, JsonSerializer> JsonSerializerConfigurer { get; private set; }
        internal Action AddAutoMapper { get; private set; }
        internal Action AddServiceBus { get; private set; }

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

        public ElsaOptions UseStorage(Func<IBlobStorage> factory) => UseStorage(_ => factory());

        public ElsaOptions UseStorage(Func<IServiceProvider, IBlobStorage> factory)
        {
            StorageFactory = factory;
            return this;
        }
        
        public ElsaOptions UseAutoMapper(Action addAutoMapper)
        {
            AddAutoMapper = addAutoMapper;
            return this;
        }

        public ElsaOptions UseServiceBus(Action addServiceBus)
        {
            AddServiceBus = addServiceBus;
            return this;
        }

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

        private static RebusConfigurer ConfigureInMemoryServiceBus(RebusConfigurer rebus, IServiceProvider serviceProvider)
        {
            var subscriberStore = serviceProvider.GetRequiredService<InMemorySubscriberStore>();
            var dataStore = serviceProvider.GetRequiredService<InMemDataStore>();
            var network = serviceProvider.GetRequiredService<InMemNetwork>();
            
            return rebus
                .Logging(logging => logging.ColoredConsole(LogLevel.Debug))
                .Subscriptions(s => s.StoreInMemory(subscriberStore))
                .DataBus(s => s.StoreInMemory(dataStore))
                .Routing(r => r.TypeBased())
                .Transport(t => t.UseInMemoryTransport(network, "elsa_publisher"));
        }
    }
}