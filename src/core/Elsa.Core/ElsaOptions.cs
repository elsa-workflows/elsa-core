using System;
using System.Data;
using Elsa.Caching;
using Elsa.DistributedLock;
using Elsa.Extensions;
using Elsa.Serialization;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rebus.DataBus.InMem;
using Rebus.Logging;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
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
            JsonSerializerConfigurer = (sp, serializer) => { };

            AddAutoMapper = () =>
            {
                services.AddAutoMapper(ServiceLifetime.Singleton);
                services.AddSingleton(sp => sp.CreateAutoMapperConfiguration());
            };

            services.AddSingleton<InMemNetwork>();
            services.AddSingleton<InMemorySubscriberStore>();
            services.AddSingleton<InMemDataStore>();
            
            ConfigureServiceBusEndpoint = ConfigureInMemoryServiceBusEndpoint;

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
        internal Action<ServiceBusEndpointConfigurationContext> ConfigureServiceBusEndpoint { get; private set; }

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

        public ElsaOptions UseServiceBus(Action<ServiceBusEndpointConfigurationContext> setup)
        {
            ConfigureServiceBusEndpoint = setup;
            return this;
        }

        private static void ConfigureInMemoryServiceBusEndpoint(ServiceBusEndpointConfigurationContext context)
        {
            var serviceProvider = context.ServiceProvider;
            var transport = serviceProvider.GetService<InMemNetwork>();
            var store = serviceProvider.GetRequiredService<InMemorySubscriberStore>();
            var queueName = context.QueueName;

            context.Configurer
                .Logging(l => l.ColoredConsole(LogLevel.Info))
                .Subscriptions(s => s.StoreInMemory(store))
                .Transport(t => t.UseInMemoryTransport(transport, queueName))
                .Routing(r => r.TypeBased().Map(context.MessageTypeMap));
        }
    }
}