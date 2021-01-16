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
            
            Func<MessageReceivedTrigger, bool> predicate = string.IsNullOrWhiteSpace(message.CorrelationId)
                ? x => x.QueueName == _messageReceiver.Path && x.CorrelationId == null
                : x => x.QueueName == _messageReceiver.Path && x.CorrelationId == message.CorrelationId;
            
            await workflowRunner.TriggerWorkflowsAsync(
                predicate,
                message,
                message.CorrelationId,
                cancellationToken: cancellationToken);
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs e)
        {
            switch (e.Exception)
            {
                case MessageLockLostException:
                    _logger.LogDebug( e.Exception,"Message lock lost");
                    break;
                case ServiceBusCommunicationException:
                    _logger.LogDebug(e.Exception, "Lost service bus communication");
                    break;
                default:
                    _logger.LogError(e.Exception, "Unhandled exception");
                    break;
            }

            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync() => await _messageReceiver.CloseAsync();
    }
}