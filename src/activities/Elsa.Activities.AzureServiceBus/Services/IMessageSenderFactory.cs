using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public interface IMessageSenderFactory
    {
        Task<ServiceBusSender> CreateQueueSenderAsync(string queue, CancellationToken cancellationToken = default);
        Task<ServiceBusSender> CreateTopicSenderAsync(string topic, CancellationToken cancellationToken = default);
    }

    public static class MessageSenderFactoryExtensions
    {
        /// <summary>
        /// Create a sender for the specified queue or the specified topic.
        /// </summary>
        public static async Task<ServiceBusSender> CreateSenderAsync(this IMessageSenderFactory factory, string? queue, string? topic, CancellationToken cancellationToken = default)
        {
            if (queue == null && topic == null)
                throw new ArgumentException("Neither queue nor topic was specified. Specify one of them.");
            
            if(queue != null && topic != null)
                throw new ArgumentException("Both queue and topic was specified. Specify just one of them.");

            return queue != null ? await factory.CreateQueueSenderAsync(queue, cancellationToken) : await factory.CreateTopicSenderAsync(topic!, cancellationToken);
        }
    }
}