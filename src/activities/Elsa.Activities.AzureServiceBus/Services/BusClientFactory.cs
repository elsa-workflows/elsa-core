using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public class BusClientFactory : IQueueMessageSenderFactory, IQueueMessageReceiverClientFactory, ITopicMessageReceiverFactory, ITopicMessageSenderFactory
    {
        private readonly ServiceBusConnection _connection;
        private readonly ManagementClient _managementClient;
        private readonly IDictionary<string, ISenderClient> _senders = new Dictionary<string, ISenderClient>();
        private readonly IDictionary<string, IReceiverClient> _receivers = new Dictionary<string, IReceiverClient>();
        private readonly SemaphoreSlim _semaphore = new(1);

        public BusClientFactory(ServiceBusConnection connection, ManagementClient managementClient)
        {
            _connection = connection;
            _managementClient = managementClient;
        }

        public async Task<ISenderClient> GetSenderAsync(string queueName, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                if (_senders.TryGetValue(queueName, out var messageSender))
                    return messageSender;

                await EnsureQueueExistsAsync(queueName, cancellationToken);
                var newMessageSender = new MessageSender(_connection, queueName);
                _senders.Add(queueName, newMessageSender);
                return newMessageSender;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IReceiverClient> GetReceiverAsync(string queueName, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);

            if (_receivers.TryGetValue(queueName, out var messageReceiver))
                return messageReceiver;

            try
            {
                await EnsureQueueExistsAsync(queueName, cancellationToken);
                var newMessageReceiver = new MessageReceiver(_connection, queueName);
                _receivers.Add(queueName, newMessageReceiver);
                return newMessageReceiver;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DisposeReceiverAsync(IReceiverClient receiverClient, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            var key = GetKeyFor(receiverClient);

            try
            {
                _receivers.Remove(key);
                await receiverClient.UnregisterMessageHandlerAsync(TimeSpan.FromSeconds(1));
                await receiverClient.CloseAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task EnsureQueueExistsAsync(string queueName, CancellationToken cancellationToken)
        {
            if (await _managementClient.QueueExistsAsync(queueName, cancellationToken))
                return;

            await _managementClient.CreateQueueAsync(queueName, cancellationToken);
        }

        public async Task<ISenderClient> GetTopicSenderAsync(string topicName, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                if (_senders.TryGetValue(topicName, out var messageSender))
                    return messageSender;

                await EnsureTopicExistsAsync(topicName, cancellationToken);
                var newMessageSender = new MessageSender(_connection, topicName);
                _senders.Add(topicName, newMessageSender);
                return newMessageSender;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IReceiverClient> GetTopicReceiverAsync(string topicName, string subscriptionName, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);

            var key = $"{topicName}:{subscriptionName}";

            if (_receivers.TryGetValue(key, out var messageReceiver))
                return messageReceiver;

            try
            {
                await EnsureTopicAndSubscriptionExistsAsync(topicName, subscriptionName, cancellationToken);

                var newTopicMessageReceiver = new SubscriptionClient(
                    _connection,
                    topicName, 
                    subscriptionName, 
                    ReceiveMode.PeekLock, 
                    RetryPolicy.Default);

                _receivers.Add(key, newTopicMessageReceiver);
                return newTopicMessageReceiver;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task EnsureTopicExistsAsync(string topicName, CancellationToken cancellationToken)
        {
            if (!await _managementClient.TopicExistsAsync(topicName, cancellationToken))
                await _managementClient.CreateTopicAsync(topicName, cancellationToken);
        }

        private async Task EnsureTopicAndSubscriptionExistsAsync(string topicName, string subscriptionName, CancellationToken cancellationToken)
        {
            await EnsureTopicExistsAsync(topicName, cancellationToken);

            if (!await _managementClient.SubscriptionExistsAsync(topicName, subscriptionName, cancellationToken))
                await _managementClient.CreateSubscriptionAsync(topicName, subscriptionName, cancellationToken);
        }
        
        private static string GetKeyFor(IReceiverClient receiverClient) =>
            receiverClient switch
            {
                IMessageReceiver messageReceiver => messageReceiver.Path,
                ISubscriptionClient subscriptionClient => $"{subscriptionClient.TopicPath}:{subscriptionClient.SubscriptionName}",
                _ => throw new ArgumentOutOfRangeException(nameof(receiverClient), receiverClient, null)
            };
    }
}