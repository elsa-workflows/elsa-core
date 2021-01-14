using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Triggers;
using Elsa.Services;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public class QueueWorker : IAsyncDisposable
    {
        private readonly IMessageReceiver _messageReceiver;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public QueueWorker(IMessageReceiver messageReceiver, IServiceProvider serviceProvider, ILogger<QueueWorker> logger)
        {
            _messageReceiver = messageReceiver;
            _serviceProvider = serviceProvider;
            _logger = logger;

            _messageReceiver.RegisterMessageHandler(OnMessageReceived, new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                AutoComplete = false,
                MaxConcurrentCalls = 100
            });
        }

        private async Task OnMessageReceived(Message message, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Message received with ID {MessageId}", message.MessageId);
            await TriggerWorkflowsAsync(message, cancellationToken);
            await _messageReceiver.CompleteAsync(message.SystemProperties.LockToken);
        }

        private async Task TriggerWorkflowsAsync(Message message, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var workflowRunner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();

            await workflowRunner.TriggerWorkflowsAsync<MessageReceivedTrigger>(
                x => x.QueueName == _messageReceiver.Path && (string.Equals(x.CorrelationId, message.CorrelationId)),
                message,
                message.CorrelationId,
                cancellationToken: cancellationToken);
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs e)
        {
            var context = e.ExceptionReceivedContext;

            switch (e.Exception)
            {
                case MessageLockLostException:
                case ServiceBusCommunicationException:
                    _logger.LogDebug(e.Exception.Message);
                    break;
                default:
                    _logger.LogError("Message handler encountered an exception {Exception}.", e.Exception);
                    _logger.LogError("Exception context for troubleshooting:");
                    _logger.LogError("- Endpoint: {Endpoint}", context.Endpoint);
                    _logger.LogError("- Entity Path: {EntityPath}", context.EntityPath);
                    _logger.LogError("- Executing Action: {Action}", context.Action);
                    break;
            }

            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync() => await _messageReceiver.CloseAsync();
    }
}