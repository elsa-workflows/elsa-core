using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public class MessageSenderFactory : IMessageSenderFactory
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ServiceBusAdministrationClient _administrationClient;

        public MessageSenderFactory(ServiceBusClient serviceBusClient, ServiceBusAdministrationClient administrationClient)
        {
            _serviceBusClient = serviceBusClient;
            _administrationClient = administrationClient;
        }

        public async Task<ServiceBusSender> CreateQueueSenderAsync(string queueName, CancellationToken cancellationToken)
        {
            await EnsureQueueExistsAsync(queueName, cancellationToken);
            return _serviceBusClient.CreateSender(queueName);
        }
        
        public async Task<ServiceBusSender> CreateTopicSenderAsync(string topicName, CancellationToken cancellationToken)
        {
            await EnsureTopicExistsAsync(topicName, cancellationToken);
            return _serviceBusClient.CreateSender(topicName);
        }
        
        private async Task EnsureTopicExistsAsync(string topicName, CancellationToken cancellationToken)
        {
            if (!await _administrationClient.TopicExistsAsync(topicName, cancellationToken))
                await _administrationClient.CreateTopicAsync(topicName, cancellationToken);
        }

        private async Task EnsureQueueExistsAsync(string queueName, CancellationToken cancellationToken)
        {
            if (await _administrationClient.QueueExistsAsync(queueName, cancellationToken))
                return;

            await _administrationClient.CreateQueueAsync(queueName, cancellationToken);
        }
    }
}