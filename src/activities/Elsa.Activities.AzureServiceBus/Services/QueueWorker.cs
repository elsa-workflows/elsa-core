using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Models;
using Elsa.Activities.AzureServiceBus.Triggers;
using Elsa.DistributedLock;
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
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ILogger _logger;

        public QueueWorker(IMessageReceiver messageReceiver, IServiceProvider serviceProvider, IDistributedLockProvider distributedLockProvider, ILogger<QueueWorker> logger)
        {
            _messageReceiver = messageReceiver;
            _serviceProvider = serviceProvider;
            _distributedLockProvider = distributedLockProvider;
            _logger = logger;

            _messageReceiver.RegisterMessageHandler(OnMessageReceived, new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                AutoComplete = false,
                MaxConcurrentCalls = 5
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
            var workflowRunner = scope.ServiceProvider.GetRequiredService<IWorkflowQueue>();
            var queueName = _messageReceiver.Path;
            var correlationId = message.CorrelationId;
            
            var model = new MessageModel
            {
                Body = message.Body,
                CorrelationId = message.CorrelationId,
                ContentType = message.ContentType,
                Label = message.Label,
                To = message.To,
                MessageId = message.MessageId,
                PartitionKey = message.PartitionKey,
                ViaPartitionKey = message.ViaPartitionKey,
                ReplyTo = message.ReplyTo,
                SessionId = message.SessionId,
                ExpiresAtUtc = message.ExpiresAtUtc,
                TimeToLive = message.TimeToLive,
                ReplyToSessionId = message.ReplyToSessionId,
                ScheduledEnqueueTimeUtc = message.ScheduledEnqueueTimeUtc
            };

            async Task TriggerNewWorkflowAsync() =>
                await workflowRunner.EnqueueWorkflowsAsync<MessageReceivedTrigger>(
                    x => x.QueueName == queueName && x.CorrelationId == null,
                    model,
                    correlationId,
                    cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(correlationId))
            {
                await TriggerNewWorkflowAsync();
                return;
            }

            var lockKey = $"azure-service-bus:{queueName}:correlation-{correlationId}";
            var stopwatch = new Stopwatch();

            _logger.LogDebug("Acquiring lock {LockKey}", lockKey);
            stopwatch.Start();

            if (!await _distributedLockProvider.AcquireLockAsync(lockKey, cancellationToken))
            {
                _logger.LogDebug("Lock {LockKey} already taken", lockKey);
                return;
            }

            try
            {
                var workflowSelector = scope.ServiceProvider.GetRequiredService<IWorkflowSelector>();
                var workflowInstanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
                var correlatedWorkflowInstanceCount = await workflowInstanceStore.CountAsync(new CorrelationIdSpecification<WorkflowInstance>(model.CorrelationId), cancellationToken);

                if (correlatedWorkflowInstanceCount > 0)
                {
                    // Trigger existing workflows (if blocked on this message).
                    _logger.LogDebug("{WorkflowInstanceCount} existing workflows found with correlation ID '{CorrelationId}'. Resuming them", correlatedWorkflowInstanceCount, correlationId);
                    var existingWorkflows = await workflowSelector.SelectWorkflowsAsync<MessageReceivedTrigger>(x => x.QueueName == queueName && x.CorrelationId == model.CorrelationId, cancellationToken).ToList();
                    await workflowRunner.EnqueueWorkflowsAsync(existingWorkflows, model, model.CorrelationId, cancellationToken: cancellationToken);
                }
                else
                {
                    // Trigger new workflow.
                    _logger.LogDebug("No existing workflows found with correlation ID '{CorrelationId}'. Starting new workflow", correlationId);
                    await TriggerNewWorkflowAsync();
                }
            }
            finally
            {
                await _distributedLockProvider.ReleaseLockAsync(lockKey, cancellationToken);
                stopwatch.Stop();
                _logger.LogDebug("Lock held for {ElapseTime}", stopwatch.Elapsed);
            }
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs e)
        {
            switch (e.Exception)
            {
                case MessageLockLostException:
                    _logger.LogDebug(e.Exception, "Message lock lost");
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