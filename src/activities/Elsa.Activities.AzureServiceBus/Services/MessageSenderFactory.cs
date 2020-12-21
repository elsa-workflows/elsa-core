using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Extensions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public class MessageSenderFactory : IMessageSenderFactory
    {
        private readonly ServiceBusConnection _connection;
        private readonly ManagementClient _managementClient;
        private readonly IDictionary<string, IMessageSender> _senders = new Dictionary<string, IMessageSender>();
        private readonly SemaphoreSlim _semaphore = new(1);
        
        public MessageSenderFactory(ServiceBusConnection connection, ManagementClient managementClient)
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
                
                await _managementClient.EnsureQueueExistsAsync(queueName, cancellationToken);
                var newMessageSender = new MessageSender(_connection, queueName);
                _senders.Add(queueName, newMessageSender);
                return newMessageSender;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}