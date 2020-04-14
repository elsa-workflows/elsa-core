using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Elsa.AzureServiceBus.Models;
using Elsa.AzureServiceBus.Notifications;
using Elsa.AzureServiceBus.Options;

namespace Elsa.AzureServiceBus.Services
{
    public class ServiceBusConsumer : IServiceBusConsumer
    {
        private readonly IMessageHandlerMediatorService _messageProcessMediator;

        private readonly IReadOnlyDictionary<string, QueueClient> _queueClients;

        private readonly ILogger _logger;

        public ServiceBusConsumer(IMessageHandlerMediatorService messageProcessMediator,
            IOptions<ServiceBusOptions> options,
            ILogger<ServiceBusConsumer> logger)
        {
            _messageProcessMediator = messageProcessMediator;
            _logger = logger;

            _queueClients = options.Value.Consumer.Select(q => new { q.Name, Client = new QueueClient(q.ServiceBusConnectionString, q.QueueName) }).ToDictionary(k => k.Name, v => v.Client);
        }

        public void RegisterOnMessageHandlerAndReceiveMessages()
        {
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = false
            };

            foreach (var qc in _queueClients)
            {
                qc.Value.RegisterMessageHandler((message, token) =>
                {
                    return ProcessMessagesAsync(qc.Key, qc.Value, message, token);
                }, messageHandlerOptions); ;
            }
        }

        private async Task ProcessMessagesAsync(string name, QueueClient client, Message message, CancellationToken token)
        {
            MessageBody messageBody = null;
            try
            {
                messageBody = JsonConvert.DeserializeObject<MessageBody>(Encoding.UTF8.GetString(message.Body));

                await _messageProcessMediator.Process(new ProcessMessageNotification { ConsumerName = name, QueueName = client.QueueName, Message = messageBody }, token);

                await client.CompleteAsync(message.SystemProperties.LockToken);

            }
            catch (Exception e)
            {

                _logger.LogError(e, "Service bus consumer message handler encountered an exception");

                _logger.LogDebug($"- MessageId: {message.MessageId}");

                throw;
            }
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            _logger.LogError(exceptionReceivedEventArgs.Exception, "Message handler encountered an exception");
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;

            _logger.LogDebug($"- Endpoint: {context.Endpoint}");
            _logger.LogDebug($"- Entity Path: {context.EntityPath}");
            _logger.LogDebug($"- Executing Action: {context.Action}");

            return Task.CompletedTask;
        }

        public async Task CloseQueueAsync()
        {
            foreach (var client in _queueClients.Values)
            {
                await client.CloseAsync();
            }
        }
    }
}
