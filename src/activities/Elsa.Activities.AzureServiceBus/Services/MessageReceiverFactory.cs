using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Extensions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public class MessageReceiverFactory : IMessageReceiverFactory
    {
        private readonly ServiceBusConnection _connection;
        private readonly ManagementClient _managementClient;
        private readonly IDictionary<string, IMessageReceiver> _receivers = new Dictionary<string, IMessageReceiver>();
        private readonly SemaphoreSlim _semaphore = new(1);

        public MessageReceiverFactory(ServiceBusConnection connection, ManagementClient managementClient)
        {
            _connection = connection;
            _managementClient = managementClient;
        }

        public async Task<IMessageReceiver> GetReceiverAsync(string queueName, CancellationToken cancellationToken)
        {
            if (_receivers.TryGetValue(queueName, out var messageReceiver))
                return messageReceiver;

            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                await _managementClient.EnsureQueueExistsAsync(queueName, cancellationToken);
                var newMessageReceiver = new MessageReceiver(_connection, queueName);
                _receivers.Add(queueName, newMessageReceiver);
                return newMessageReceiver;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}