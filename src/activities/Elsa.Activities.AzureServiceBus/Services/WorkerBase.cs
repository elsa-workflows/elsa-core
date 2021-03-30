using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Models;
using Elsa.Activities.AzureServiceBus.Options;
using Elsa.Bookmarks;
using Elsa.Dispatch;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public abstract class WorkerBase : IAsyncDisposable
    {
        // TODO: Design multi-tenancy. 
        private const string TenantId = default;

        private readonly ICorrelatingWorkflowDispatcher _workflowDispatcher;
        private readonly ILogger _logger;

        protected WorkerBase(
            IReceiverClient receiverClient,
            ICorrelatingWorkflowDispatcher workflowDispatcher,
            IOptions<AzureServiceBusOptions> options,
            ILogger logger)
        {
            ReceiverClient = receiverClient;
            _workflowDispatcher = workflowDispatcher;
            _logger = logger;

            ReceiverClient.RegisterMessageHandler(OnMessageReceived, new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                AutoComplete = false,
                MaxConcurrentCalls = options.Value.MaxConcurrentCalls
            });
        }
        
        protected IReceiverClient ReceiverClient { get; }
        protected abstract string ActivityType { get; }

        public async ValueTask DisposeAsync() => await ReceiverClient.CloseAsync();

        protected abstract IBookmark CreateBookmark(Message message);
        protected abstract IBookmark CreateTrigger(Message message);

        private async Task TriggerWorkflowsAsync(Message message, CancellationToken cancellationToken)
        {
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
            
            var queueName = ReceiverClient.Path;
            var bookmark = CreateBookmark(message);
            var trigger = CreateTrigger(message);
            await _workflowDispatcher.DispatchAsync(new ExecuteCorrelatedWorkflowRequest(correlationId, bookmark, trigger, ActivityType, model, TenantId: TenantId), cancellationToken);
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
        
        private async Task OnMessageReceived(Message message, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Message received with ID {MessageId}", message.MessageId);
            await TriggerWorkflowsAsync(message, cancellationToken);
            await ReceiverClient.CompleteAsync(message.SystemProperties.LockToken);
        }
    }
}