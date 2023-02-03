using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Elsa.Activities.AzureServiceBus.Bookmarks;
using Elsa.Activities.AzureServiceBus.Models;
using Elsa.Activities.AzureServiceBus.Options;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public class Worker : IAsyncDisposable
    {
        private const string? TenantId = default;
        private readonly Func<Worker, ProcessErrorEventArgs, Task> _errorCallback;
        private readonly ServiceBusAdministrationClient _administrationClient;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger _logger;
        private readonly ServiceBusProcessor _processor;
        private readonly IAzureServiceBusTenantIdResolver _tenantIdResolver;

        public Worker(
            string queueOrTopic,
            string? subscription,
            Func<Worker, ProcessErrorEventArgs, Task> errorCallback,
            ServiceBusClient serviceBusClient,
            ServiceBusAdministrationClient administrationClient,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<AzureServiceBusOptions> options,
            IAzureServiceBusTenantIdResolver tenantIdResolver,
            ILogger<Worker> logger)
        {
            QueueOrTopic = queueOrTopic;
            Subscription = subscription == "" ? null : subscription;
            ServiceBusClient = serviceBusClient;
            _errorCallback = errorCallback;
            _administrationClient = administrationClient;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _tenantIdResolver = tenantIdResolver;

            var processorOptions = new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = options.Value.MaxConcurrentCalls
            };

            _processor = string.IsNullOrEmpty(subscription) ? serviceBusClient.CreateProcessor(queueOrTopic, processorOptions) : serviceBusClient.CreateProcessor(queueOrTopic, subscription, processorOptions);
            _processor.ProcessMessageAsync += OnMessageReceivedAsync;
            _processor.ProcessErrorAsync += OnErrorAsync;
        }

        public string QueueOrTopic { get; }
        public string? Subscription { get; }
        public HashSet<string> Tags { get; } = new();
        protected ServiceBusClient ServiceBusClient { get; set; }
        private string ActivityType => Subscription == null ? nameof(AzureServiceBusQueueMessageReceived) : nameof(AzureServiceBusTopicMessageReceived);

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (Subscription == null)
            {
                if (!await _administrationClient.QueueExistsAsync(QueueOrTopic, cancellationToken))
                    await _administrationClient.CreateQueueAsync(QueueOrTopic, cancellationToken);
            }
            else
            {
                if (!await _administrationClient.TopicExistsAsync(QueueOrTopic, cancellationToken))
                    await _administrationClient.CreateTopicAsync(QueueOrTopic, cancellationToken);

                if (!await _administrationClient.SubscriptionExistsAsync(QueueOrTopic, Subscription, cancellationToken))
                    await _administrationClient.CreateSubscriptionAsync(QueueOrTopic, Subscription, cancellationToken);
            }

            await _processor.StartProcessingAsync(cancellationToken);
        }

        public async ValueTask DisposeAsync() => await _processor.DisposeAsync();

        private async Task OnMessageReceivedAsync(ProcessMessageEventArgs args)
        {
            var message = args.Message;
            _logger.LogDebug("Message received with ID {MessageId}", message.MessageId);
            await TriggerWorkflowsAsync(new ServiceBusMessage(message), CancellationToken.None);
        }

        private async Task OnErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "An error occurred while processing {EntityPath}", args.EntityPath);
            await _errorCallback(this, args);
        }

        private async Task TriggerWorkflowsAsync(ServiceBusMessage message, CancellationToken cancellationToken)
        {
            var tenantId = await _tenantIdResolver.ResolveAsync(message, QueueOrTopic, Subscription, Tags, cancellationToken);
            var correlationId = message.CorrelationId;

            var model = new MessageModel
            {
                Body = message.Body.ToArray(),
                CorrelationId = message.CorrelationId,
                ContentType = message.ContentType,
                Label = message.Subject,
                To = message.To,
                MessageId = message.MessageId,
                PartitionKey = message.PartitionKey,
                ViaPartitionKey = message.TransactionPartitionKey,
                ReplyTo = message.ReplyTo,
                SessionId = message.SessionId,
                TimeToLive = message.TimeToLive,
                ReplyToSessionId = message.ReplyToSessionId,
                ScheduledEnqueueTimeUtc = message.ScheduledEnqueueTime.UtcDateTime,
                UserProperties = new Dictionary<string, object>(message.ApplicationProperties),
            };

            var bookmark = new MessageReceivedBookmark(QueueOrTopic, Subscription);
            var launchContext = new WorkflowsQuery(ActivityType, bookmark, correlationId, TenantId: tenantId);

            using var scope = _serviceScopeFactory.CreateScope();
            var workflowLaunchpad = scope.ServiceProvider.GetRequiredService<IWorkflowLaunchpad>();
            await workflowLaunchpad.CollectAndDispatchWorkflowsAsync(launchContext, new WorkflowInput(model), cancellationToken);
        }
    }
}
