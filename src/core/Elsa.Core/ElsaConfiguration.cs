using System;
using Elsa.Caching;
using Elsa.DistributedLock;
using Elsa.Persistence;
using Elsa.Persistence.InMemory;
using Elsa.Serialization;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rebus.DataBus.InMem;
using Rebus.Logging;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.Transport.InMem;
using Storage.Net;
using Storage.Net.Blobs;

namespace Elsa
{
    public class ElsaConfiguration
    {
        public ElsaConfiguration(IServiceCollection services)
        {
            Services = services;

            WorkflowDefinitionStoreFactory = sp => ActivatorUtilities.CreateInstance<InMemoryWorkflowDefinitionStore>(sp);
            WorkflowInstanceStoreFactory = sp => ActivatorUtilities.CreateInstance<InMemoryWorkflowInstanceStore>(sp);
            WorkflowExecutionLogStoreFactory = sp => ActivatorUtilities.CreateInstance<InMemoryWorkflowExecutionLogStore>(sp);
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
        internal Func<IServiceProvider, IBlobStorage> StorageFactory { get; set; }
        internal Func<IServiceProvider, IWorkflowDefinitionStore> WorkflowDefinitionStoreFactory { get; set; }
        internal Func<IServiceProvider, IWorkflowInstanceStore> WorkflowInstanceStoreFactory { get; set; }
        internal Func<IServiceProvider, IWorkflowExecutionLogStore> WorkflowExecutionLogStoreFactory { get; set; }
        internal Func<IServiceProvider, IDistributedLockProvider> DistributedLockProviderFactory { get; private set; }
        internal Func<IServiceProvider, ISignal> SignalFactory { get; private set; }
        internal Func<IServiceProvider, JsonSerializer> CreateJsonSerializer { get; private set; }
        internal Action<IServiceProvider, JsonSerializer> JsonSerializerConfigurer { get; private set; }
        internal Action AddAutoMapper { get; private set; }
        internal Action<ServiceBusEndpointConfigurationContext> ConfigureServiceBusEndpoint { get; private set; }

        public ElsaConfiguration UseDistributedLockProvider(Func<IServiceProvider, IDistributedLockProvider> factory)
        {
            DistributedLockProviderFactory = factory;
            return this;
        }

        public ElsaConfiguration UseSignal(Func<IServiceProvider, ISignal> factory)
        {
            SignalFactory = factory;
            return this;
        }

        public ElsaConfiguration UseStorage(Func<IBlobStorage> factory) => UseStorage(_ => factory());

        public ElsaConfiguration UseStorage(Func<IServiceProvider, IBlobStorage> factory)
        {
            StorageFactory = factory;
            return this;
        }
        
        public ElsaConfiguration UseWorkflowDefinitionStore(Func<IServiceProvider, IWorkflowDefinitionStore> factory)
        {
            WorkflowDefinitionStoreFactory = factory;
            return this;
        }
        
        public ElsaConfiguration UseWorkflowInstanceStore(Func<IServiceProvider, IWorkflowInstanceStore> factory)
        {
            WorkflowInstanceStoreFactory = factory;
            return this;
        }
        
        public ElsaConfiguration UseWorkflowExecutionLogStore(Func<IServiceProvider, IWorkflowExecutionLogStore> factory)
        {
            WorkflowExecutionLogStoreFactory = factory;
            return this;
        }

        public ElsaConfiguration UseAutoMapper(Action addAutoMapper)
        {
            AddAutoMapper = addAutoMapper;
            return this;
        }

        public ElsaConfiguration UseJsonSerializer(Func<IServiceProvider, JsonSerializer> factory)
        {
            CreateJsonSerializer = factory;
            return this;
        }

        public ElsaConfiguration ConfigureJsonSerializer(Action<IServiceProvider, JsonSerializer> configure)
        {
            JsonSerializerConfigurer = configure;
            return this;
        }

        public ElsaConfiguration UseServiceBus(Action<ServiceBusEndpointConfigurationContext> setup)
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