using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Bookmarks;
using Elsa.Activities.AzureServiceBus.Models;
using Elsa.Activities.AzureServiceBus.Options;
using Elsa.Bookmarks;
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
using Microsoft.Extensions.Options;
using Open.Linq.AsyncExtensions;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public class QueueWorker : IAsyncDisposable
    {
        // TODO: Figure out how to start jobs across multiple tenants / how to get a list of all tenants. 
        private const string TenantId = default;

        private readonly IMessageReceiver _messageReceiver;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ILogger _logger;

        public QueueWorker(
            IMessageReceiver messageReceiver,
            IServiceScopeFactory serviceScopeFactory,
            IDistributedLockProvider distributedLockProvider,
            IOptions<AzureServiceBusOptions> options,
            ILogger<QueueWorker> logger)
        {
            _messageReceiver = messageReceiver;
            _serviceScopeFactory = serviceScopeFactory;
            _distributedLockProvider = distributedLockProvider;
            _logger = logger;

            _messageReceiver.RegisterMessageHandler(OnMessageReceived, new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                AutoComplete = false,
                MaxConcurrentCalls = options.Value.MaxConcurrentCalls
            });
        }

        public async ValueTask DisposeAsync() => await _messageReceiver.CloseAsync();

        private async Task OnMessageReceived(Message message, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Message received with ID {MessageId}", message.MessageId);
            await TriggerWorkflowsAsync(message, cancellationToken);
            await _messageReceiver.CompleteAsync(message.SystemProperties.LockToken);
        }

        private async Task TriggerWorkflowsAsync(Message message, CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var workflowQueue = scope.ServiceProvider.GetRequiredService<IWorkflowQueue>();
            var queueName = _messageReceiver.Path;
            var correlationId = message.CorrelationId;
            var triggerFinder = scope.ServiceProvider.GetRequiredService<ITriggerFinder>();

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
            
            async Task TriggerNewWorkflowAsync()
            {
                var bookmark = new MessageReceivedBookmark(queueName);
                var triggers = await triggerFinder.FindTriggersAsync<AzureServiceBusMessageReceived>(bookmark, TenantId, cancellationToken);
                
                foreach (var trigger in triggers)
                {
                    var workflowBlueprint = trigger.WorkflowBlueprint;
                    await workflowQueue.EnqueueWorkflowDefinition(workflowBlueprint.Id, workflowBlueprint.TenantId, trigger.ActivityId, model, correlationId, null, cancellationToken);
                }
            }

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
                var bookmarkFinder = scope.ServiceProvider.GetRequiredService<IBookmarkFinder>();
                var workflowInstanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
                var correlatedWorkflowInstanceCount = await workflowInstanceStore.CountAsync(new CorrelationIdSpecification<WorkflowInstance>(model.CorrelationId), cancellationToken);

                if (correlatedWorkflowInstanceCount > 0)
                {
                    // Trigger existing workflows (if blocked on this message).
                    _logger.LogDebug("{WorkflowInstanceCount} existing workflows found with correlation ID '{CorrelationId}'. Resuming them", correlatedWorkflowInstanceCount, correlationId);
                    var bookmark = new MessageReceivedBookmark(queueName, correlationId);
                    var existingWorkflows = await bookmarkFinder.FindBookmarksAsync<AzureServiceBusMessageReceived>(bookmark, TenantId, cancellationToken).ToList();
                    await workflowQueue.EnqueueWorkflowsAsync(existingWorkflows, model, model.CorrelationId, cancellationToken: cancellationToken);
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
    }
}