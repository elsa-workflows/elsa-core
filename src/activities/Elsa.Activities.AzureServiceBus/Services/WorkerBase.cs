using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Models;
using Elsa.Activities.AzureServiceBus.Options;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public abstract class WorkerBase : IAsyncDisposable
    {
        // TODO: Design multi-tenancy. 
        private const string? TenantId = default;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly Func<IReceiverClient, Task> _disposeReceiverAction;
        private readonly ILogger _logger;

        protected WorkerBase(
            string tag,
            IReceiverClient receiverClient,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<AzureServiceBusOptions> options,
            Func<IReceiverClient, Task> disposeReceiverAction,
            ILogger logger)
        {
            Tag = tag;
            ReceiverClient = receiverClient;
            _serviceScopeFactory = serviceScopeFactory;
            _disposeReceiverAction = disposeReceiverAction;
            _logger = logger;

            ReceiverClient.RegisterMessageHandler(OnMessageReceived, new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                AutoComplete = false,
                MaxConcurrentCalls = options.Value.MaxConcurrentCalls
            });
        }

        public string Tag { get; }
        protected IReceiverClient ReceiverClient { get; }
        protected abstract string ActivityType { get; }
        public async ValueTask DisposeAsync() => await _disposeReceiverAction(ReceiverClient);

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
                ScheduledEnqueueTimeUtc = message.ScheduledEnqueueTimeUtc,
                UserProperties = new Dictionary<string, object>(message.UserProperties),
            };

            var bookmark = CreateBookmark(message);
            var launchContext = new WorkflowsQuery(ActivityType, bookmark, correlationId);

            using var scope = _serviceScopeFactory.CreateScope();
            var workflowLaunchpad = scope.ServiceProvider.GetRequiredService<IWorkflowLaunchpad>();
            await workflowLaunchpad.CollectAndDispatchWorkflowsAsync(launchContext, new WorkflowInput(model), cancellationToken);
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
            await TriggerWorkflowsAsync(message, CancellationToken.None);

            if (!ReceiverClient.IsClosedOrClosing)
                await ReceiverClient.CompleteAsync(message.SystemProperties.LockToken);
        }
    }
}