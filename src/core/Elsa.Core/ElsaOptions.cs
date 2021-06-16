using System;
using System.Collections.Generic;
using Elsa.Builders;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.InMemory;
using Elsa.Providers.WorkflowStorage;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Dispatch;
using Elsa.Services.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NodaTime;
using Rebus.Persistence.InMem;
using Rebus.Transport.InMem;
using Storage.Net;
using Storage.Net.Blobs;

namespace Elsa
{
    public class ElsaOptions
    {
        internal ElsaOptions()
        {
            WorkflowDefinitionStoreFactory = sp => ActivatorUtilities.CreateInstance<InMemoryWorkflowDefinitionStore>(sp);
            WorkflowInstanceStoreFactory = sp => ActivatorUtilities.CreateInstance<InMemoryWorkflowInstanceStore>(sp);
            WorkflowExecutionLogStoreFactory = sp => ActivatorUtilities.CreateInstance<InMemoryWorkflowExecutionLogStore>(sp);
            WorkflowTriggerStoreFactory = sp => ActivatorUtilities.CreateInstance<InMemoryBookmarkStore>(sp);
            WorkflowDefinitionDispatcherFactory = sp => ActivatorUtilities.CreateInstance<QueuingWorkflowDispatcher>(sp);
            WorkflowInstanceDispatcherFactory = sp => ActivatorUtilities.CreateInstance<QueuingWorkflowDispatcher>(sp);
            CorrelatingWorkflowDispatcherFactory = sp => ActivatorUtilities.CreateInstance<QueuingWorkflowDispatcher>(sp);
            StorageFactory = sp => Storage.Net.StorageFactory.Blobs.InMemory();
            JsonSerializerConfigurer = (sp, serializer) => { };
            DefaultWorkflowStorageProviderType = typeof(WorkflowInstanceWorkflowStorageProvider);
            DistributedLockingOptions = new DistributedLockingOptions();
            ConfigureServiceBusEndpoint = ConfigureInMemoryServiceBusEndpoint;

            CreateJsonSerializer = sp =>
            {
                var serializer = DefaultContentSerializer.CreateDefaultJsonSerializer();
                JsonSerializerConfigurer(sp, serializer);
                return serializer;
            };
        }

        public string ContainerName { get; set; } = Environment.MachineName;
        public ServiceFactory<IActivity> ActivityFactory { get; } = new();
        public ServiceFactory<IWorkflow> WorkflowFactory { get; } = new();
        public IEnumerable<Type> ActivityTypes => ActivityFactory.Types;

        public IList<Type> WorkflowTypes { get; } = new List<Type>();
        public IList<Type> CompetingMessageTypes { get; } = new List<Type>();
        public IList<Type> PubSubMessageTypes { get; } = new List<Type>();
        public ServiceBusOptions ServiceBusOptions { get; } = new();
        public DistributedLockingOptions DistributedLockingOptions { get; set; }

        /// <summary>
        /// The amount of time to wait before giving up on trying to acquire a lock.
        /// </summary>
        public Duration DistributedLockTimeout { get; set; } = Duration.FromHours(1);

        public Type DefaultWorkflowStorageProviderType { get; set; }

        internal Func<IServiceProvider, IBlobStorage> StorageFactory { get; set; }
        internal Func<IServiceProvider, IWorkflowDefinitionStore> WorkflowDefinitionStoreFactory { get; set; }
        internal Func<IServiceProvider, IWorkflowInstanceStore> WorkflowInstanceStoreFactory { get; set; }
        internal Func<IServiceProvider, IWorkflowExecutionLogStore> WorkflowExecutionLogStoreFactory { get; set; }
        internal Func<IServiceProvider, IBookmarkStore> WorkflowTriggerStoreFactory { get; set; }
        internal Func<IServiceProvider, JsonSerializer> CreateJsonSerializer { get; set; }
        internal Action<IServiceProvider, JsonSerializer> JsonSerializerConfigurer { get; set; }
        internal Func<IServiceProvider, IWorkflowDefinitionDispatcher> WorkflowDefinitionDispatcherFactory { get; set; }
        internal Func<IServiceProvider, IWorkflowInstanceDispatcher> WorkflowInstanceDispatcherFactory { get; set; }
        internal Func<IServiceProvider, IWorkflowDispatcher> CorrelatingWorkflowDispatcherFactory { get; set; }
        internal Action<ServiceBusEndpointConfigurationContext> ConfigureServiceBusEndpoint { get; set; }

        private static void ConfigureInMemoryServiceBusEndpoint(ServiceBusEndpointConfigurationContext context)
        {
            var serviceProvider = context.ServiceProvider;
            var transport = serviceProvider.GetService<InMemNetwork>();
            var store = serviceProvider.GetRequiredService<InMemorySubscriberStore>();
            var queueName = context.QueueName;

            context.Configurer
                .Subscriptions(s => s.StoreInMemory(store))
                .Transport(t => t.UseInMemoryTransport(transport, queueName));
        }
    }
}