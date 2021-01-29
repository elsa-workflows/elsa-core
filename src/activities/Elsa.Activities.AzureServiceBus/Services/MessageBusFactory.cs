using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public class MessageBusFactory : IMessageSenderFactory, IMessageReceiverFactory
    {
        private readonly ServiceBusConnection _connection;
        private readonly ManagementClient _managementClient;
        private readonly IDictionary<string, IMessageSender> _senders = new Dictionary<string, IMessageSender>();
        private readonly IDictionary<string, IMessageReceiver> _receivers = new Dictionary<string, IMessageReceiver>();
        private readonly SemaphoreSlim _semaphore = new(1);

        public MessageBusFactory(ServiceBusConnection connection, ManagementClient managementClient)
        {
            _connection = connection;
            _managementClient = managementClient;
        }

        public async Task<IMessageSender> GetSenderAsync(string queueName, CancellationToken cancellationToken)
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

        public async Task<IMessageReceiver> GetReceiverAsync(string queueName, CancellationToken cancellationToken)
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

        private async Task EnsureQueueExistsAsync(string queueName, CancellationToken cancellationToken)
        {
            if (await _managementClient.QueueExistsAsync(queueName, cancellationToken))
                return;

            await _managementClient.CreateQueueAsync(queueName, cancellationToken);
        }
    }
}