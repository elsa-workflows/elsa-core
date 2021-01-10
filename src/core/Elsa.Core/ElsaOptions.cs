using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoMapper;
using Elsa.Builders;
using Elsa.Caching;
using Elsa.DistributedLock;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.InMemory;
using Elsa.Serialization;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rebus.Config;
using Rebus.DataBus.InMem;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.Transport.InMem;
using Storage.Net;
using Storage.Net.Blobs;

namespace Elsa
{
    public class ElsaOptions
    {
        private readonly IList<Type> _messageTypes = new List<Type>();

        public ElsaOptions(IServiceCollection services)
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
                // The profiles are added to AddWorkflowsCore so that they are not forgotten in case the AddAutoMapper function(option) is overridden.
                services.AddAutoMapper(Enumerable.Empty<Assembly>(), serviceLifetime: ServiceLifetime.Singleton);
            };

            services.AddSingleton<InMemNetwork>();
            services.AddSingleton<InMemorySubscriberStore>();
            services.AddSingleton<InMemDataStore>();
            services.AddMemoryCache();
            
            ConfigureServiceBusEndpoint = ConfigureInMemoryServiceBusEndpoint;

            CreateJsonSerializer = sp =>
            {
                var serializer = DefaultContentSerializer.CreateDefaultJsonSerializer();
                JsonSerializerConfigurer(sp, serializer);
                return serializer;
            };
        }

        public IServiceCollection Services { get; }
        public ServiceFactory<IActivity> ActivityFactory { get; } = new();
        public ServiceFactory<IWorkflow> WorkflowFactory { get; } = new();
        public IEnumerable<Type> ActivityTypes => ActivityFactory.Types;
        public IEnumerable<Type> WorkflowTypes => WorkflowFactory.Types;
        public IEnumerable<Type> MessageTypes => _messageTypes.ToList();
        public ServiceBusOptions ServiceBusOptions { get; } = new();

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
        
        public ElsaOptions AddActivity<T>() where T : IActivity => AddActivity(typeof(T));

        public ElsaOptions AddActivity(Type activityType)
        {
            Services.AddTransient(activityType);
            Services.AddTransient(sp => (IActivity) sp.GetRequiredService(activityType));
            ActivityFactory.Add(activityType, provider => (IActivity)ActivatorUtilities.GetServiceOrCreateInstance(provider, activityType));
            return this;
        }
        
        public ElsaOptions AddActivitiesFrom(Assembly assembly)
        {
            var types = assembly.GetAllWithInterface<IActivity>();

            foreach (var type in types) 
                AddActivity(type);

            return this;
        }

        public ElsaOptions RemoveActivity<T>() where T : IActivity => RemoveActivity(typeof(T));

        public ElsaOptions RemoveActivity(Type activityType)
        {
            ActivityFactory.Remove(activityType);
            return this;
        }
        
        public ElsaOptions AddWorkflow<T>() where T : IWorkflow => AddWorkflow(typeof(T));

        public ElsaOptions AddWorkflow(Type workflowType)
        {
            Services.AddSingleton(workflowType);
            Services.AddSingleton(sp => (IWorkflow)sp.GetRequiredService(workflowType));
            WorkflowFactory.Add(workflowType, provider => (IWorkflow)ActivatorUtilities.GetServiceOrCreateInstance(provider, workflowType));
            return this;
        }
        
        public ElsaOptions AddWorkflow(IWorkflow workflow)
        {
            Services.AddSingleton(workflow);
            WorkflowFactory.Add(workflow.GetType(), workflow);
            return this;
        }
        
        public ElsaOptions AddWorkflow<T>(Func<IServiceProvider, T> workflow) where T: class, IWorkflow
        {
            Services.AddSingleton<T>(workflow);
            Services.AddSingleton<IWorkflow>(sp => sp.GetRequiredService<T>());
            WorkflowFactory.Add(typeof(T), sp => sp.GetRequiredService<T>());
            
            return this;
        }

        public ElsaOptions AddWorkflowsFrom<T>() => AddWorkflowsFrom(typeof(T).Assembly);
        
        public ElsaOptions AddWorkflowsFrom(Assembly assembly)
        {
            var types = assembly.GetAllWithInterface<IWorkflow>();

            foreach (var type in types) 
                AddWorkflow(type);

            return this;
        }

        public ElsaOptions RemoveWorkflow<T>() where T : IWorkflow => RemoveWorkflow(typeof(T));

        public ElsaOptions RemoveWorkflow(Type workflowType)
        {
            WorkflowFactory.Remove(workflowType);
            return this;
        }

        public ElsaOptions AddMessageType(Type messageType)
        {
            _messageTypes.Add(messageType);
            return this;
        }

        public ElsaOptions AddMessageType<T>() => AddMessageType(typeof(T));

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

        public ElsaOptions UseStorage(Func<IBlobStorage> factory) => UseStorage(_ => factory());

        public ElsaOptions UseStorage(Func<IServiceProvider, IBlobStorage> factory)
        {
            StorageFactory = factory;
            return this;
        }
        
        public ElsaOptions UseWorkflowDefinitionStore(Func<IServiceProvider, IWorkflowDefinitionStore> factory)
        {
            WorkflowDefinitionStoreFactory = factory;
            return this;
        }
        
        public ElsaOptions UseWorkflowInstanceStore(Func<IServiceProvider, IWorkflowInstanceStore> factory)
        {
            WorkflowInstanceStoreFactory = factory;
            return this;
        }
        
        public ElsaOptions UseWorkflowExecutionLogStore(Func<IServiceProvider, IWorkflowExecutionLogStore> factory)
        {
            WorkflowExecutionLogStoreFactory = factory;
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

        private void ConfigureInMemoryServiceBusEndpoint(ServiceBusEndpointConfigurationContext context)
        {
            var serviceProvider = context.ServiceProvider;
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var transport = serviceProvider.GetService<InMemNetwork>();
            var store = serviceProvider.GetRequiredService<InMemorySubscriberStore>();
            var queueName = context.QueueName;

            context.Configurer
                .Logging(l => l.MicrosoftExtensionsLogging(loggerFactory))
                .Subscriptions(s => s.StoreInMemory(store))
                .Transport(t => t.UseInMemoryTransport(transport, queueName))
                .Routing(r => r.TypeBased().Map(context.MessageTypeMap))
                .Options(options => options.Apply(ServiceBusOptions));
        }
    }
}