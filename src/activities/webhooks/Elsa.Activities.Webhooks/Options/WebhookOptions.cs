using System;
using System.Collections.Generic;
using Elsa.Activities.Webhooks.Persistence.InMemory;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Webhooks.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NodaTime;
using Rebus.Persistence.InMem;
using Rebus.Transport.InMem;
using Storage.Net;
using Storage.Net.Blobs;

namespace Elsa.Activities.Webhooks.Options
{
    public class WebhookOptions
    {
        public WebhookOptions()
        {
            WebhookDefinitionStoreFactory = provider => ActivatorUtilities.CreateInstance<InMemoryWebhookDefinitionStore>(provider);
            //StorageFactory = sp => Storage.Net.StorageFactory.Blobs.InMemory();
            //JsonSerializerConfigurer = (sp, serializer) => { };
            //DistributedLockingOptions = new DistributedLockingOptions();
            //ConfigureServiceBusEndpoint = ConfigureInMemoryServiceBusEndpoint;

            //CreateJsonSerializer = sp =>
            //{
            //    var serializer = DefaultContentSerializer.CreateDefaultJsonSerializer();
            //    JsonSerializerConfigurer(sp, serializer);
            //    return serializer;
            //};
        }

        //public string ContainerName { get; set; } = Environment.MachineName;
        //public IList<Type> WebhookTypes { get; } = new List<Type>();
        //public IList<Type> CompetingMessageTypes { get; } = new List<Type>();
        //public IList<Type> PubSubMessageTypes { get; } = new List<Type>();
        //public ServiceBusOptions ServiceBusOptions { get; } = new();
        //public DistributedLockingOptions DistributedLockingOptions { get; set; }
        ///// <summary>
        ///// The amount of time to wait before giving up on trying to acquire a lock.
        ///// </summary>
        //public Duration DistributedLockTimeout { get; set; } = Duration.FromHours(1);

        //internal Func<IServiceProvider, IBlobStorage> StorageFactory { get; set; }
        internal Func<IServiceProvider, IWebhookDefinitionStore> WebhookDefinitionStoreFactory { get; set; }
        //internal Func<IServiceProvider, JsonSerializer> CreateJsonSerializer { get; set; }
        //internal Action<IServiceProvider, JsonSerializer> JsonSerializerConfigurer { get; set; }
        //internal Action<ServiceBusEndpointConfigurationContext> ConfigureServiceBusEndpoint { get; set; }

        //private static void ConfigureInMemoryServiceBusEndpoint(ServiceBusEndpointConfigurationContext context)
        //{
        //    var serviceProvider = context.ServiceProvider;
        //    var transport = serviceProvider.GetService<InMemNetwork>();
        //    var store = serviceProvider.GetRequiredService<InMemorySubscriberStore>();
        //    var queueName = context.QueueName;

        //    context.Configurer
        //        .Subscriptions(s => s.StoreInMemory(store))
        //        .Transport(t => t.UseInMemoryTransport(transport, queueName));
        //}
    }
}
