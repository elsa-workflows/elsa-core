using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Triggers;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Elsa.Triggers;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;

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
            var queueName = _messageReceiver.Path;

            async Task TriggerNewWorkflowAsync()
            {
                await workflowRunner!.TriggerWorkflowsAsync<MessageReceivedTrigger>(
                    x => x.QueueName == queueName && x.CorrelationId == null,
                    message,
                    message.CorrelationId,
                    cancellationToken: cancellationToken);
            }
            
            if (string.IsNullOrWhiteSpace(message.CorrelationId))
            {
                await TriggerNewWorkflowAsync();
                return;
            }

            var workflowSelector = scope.ServiceProvider.GetRequiredService<IWorkflowSelector>();
            var workflowInstanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
            var correlatedWorkflowInstanceCount = await workflowInstanceStore.CountAsync(new CorrelationIdSpecification<WorkflowInstance>(message.CorrelationId), cancellationToken);

            if (correlatedWorkflowInstanceCount > 0)
            {
                // Trigger existing workflows (if blocked on this message).
                var existingWorkflows = await workflowSelector.SelectWorkflowsAsync<MessageReceivedTrigger>(x => x.QueueName == queueName && x.CorrelationId == message.CorrelationId, cancellationToken).ToList();
                await workflowRunner.TriggerWorkflowsAsync(existingWorkflows, message, message.CorrelationId, cancellationToken: cancellationToken);
            }
            else
            {
                // Trigger new workflow.
                await TriggerNewWorkflowAsync();
            }
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