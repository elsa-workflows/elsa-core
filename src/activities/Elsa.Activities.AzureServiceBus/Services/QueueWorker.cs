using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Bookmarks;
using Elsa.Activities.AzureServiceBus.Models;
using Elsa.Activities.AzureServiceBus.Options;
using Elsa.Bookmarks;
using Elsa.Dispatch;
using Elsa.DistributedLock;
using Elsa.DistributedLocking;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Elsa.Services.Models;
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
        private readonly ICorrelatingWorkflowDispatcher _workflowDispatcher;
        private readonly ILogger _logger;

        public QueueWorker(
            IMessageReceiver messageReceiver,
            ICorrelatingWorkflowDispatcher workflowDispatcher,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<AzureServiceBusOptions> options,
            ILogger<QueueWorker> logger)
        {
            _messageReceiver = messageReceiver;
            _workflowDispatcher = workflowDispatcher;
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
            
            var bookmark = new QueueMessageReceivedBookmark(queueName, correlationId);
            var trigger = new QueueMessageReceivedBookmark(queueName);
            var activityType = nameof(AzureServiceBusQueueMessageReceived);
            await _workflowDispatcher.DispatchAsync(new ExecuteCorrelatedWorkflowRequest(correlationId, bookmark, trigger, activityType, model, TenantId: TenantId), cancellationToken);
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