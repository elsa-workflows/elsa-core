using Azure.Messaging.ServiceBus;
using Elsa.AzureServiceBus.Activities;
using Elsa.AzureServiceBus.Models;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models.Requests;
using Microsoft.Extensions.Logging;

namespace Elsa.AzureServiceBus.Services;

/// <summary>
/// Processes messages received via a queue specified through the <see cref="MessageReceivedTriggerPayload"/>.
/// When a message is received, the appropriate workflows are executed.
/// </summary>
public class Worker : IAsyncDisposable
{
    private static readonly string BookmarkName = TypeNameHelper.GenerateTypeName<MessageReceived>();
    private readonly ServiceBusProcessor _processor;
    private readonly IWorkflowDispatcher _workflowDispatcher;
    private readonly IHasher _hasher;
    private readonly ILogger _logger;
    private int _refCount = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="Worker"/> class.
    /// </summary>
    public Worker(string queueOrTopic, string? subscription, IWorkflowDispatcher workflowDispatcher, ServiceBusClient client, IHasher hasher, ILogger<Worker> logger)
    {
        QueueOrTopic = queueOrTopic;
        Subscription = subscription == "" ? default : subscription;
        _workflowDispatcher = workflowDispatcher;
        _hasher = hasher;
        _logger = logger;

        var options = new ServiceBusProcessorOptions();
        var processor = string.IsNullOrEmpty(subscription) ? client.CreateProcessor(queueOrTopic, options) : client.CreateProcessor(queueOrTopic, subscription, options);

        processor.ProcessMessageAsync += OnMessageReceivedAsync;
        processor.ProcessErrorAsync += OnErrorAsync;
        _processor = processor;
    }

    /// <summary>
    /// The name of the queue or topic that this worker is processing.
    /// </summary>
    public string QueueOrTopic { get; }
    
    /// <summary>
    /// The name of the subscription that this worker is processing. Only valid if the worker is processing a topic.
    /// </summary>
    public string? Subscription { get; }

    /// <summary>
    /// Maintains the number of workflows that are relying on this worker. When it goes to zero, the worker will be removed.
    /// </summary>
    public int RefCount
    {
        get => _refCount;
        private set
        {
            if (value < 0)
                throw new ArgumentException("RefCount cannot be less than zero");

            _refCount = value;
        }
    }

    /// <summary>
    /// Starts the worker.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task StartAsync(CancellationToken cancellationToken = default) => await _processor.StartProcessingAsync(cancellationToken);
    
    /// <summary>
    /// Increments the ref count.
    /// </summary>
    public void IncrementRefCount() => RefCount++;
    
    /// <summary>
    /// Decrements the ref count.
    /// </summary>
    public void DecrementRefCount() => RefCount--;
    
    /// <summary>
    /// Disposes the worker.
    /// </summary>
    public async ValueTask DisposeAsync() => await _processor.DisposeAsync();
    
    private async Task OnMessageReceivedAsync(ProcessMessageEventArgs args) => await InvokeWorkflowsAsync(args.Message, args.CancellationToken);

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "An error occurred while processing {EntityPath}", args.EntityPath);
        return Task.CompletedTask;
    }

    private async Task InvokeWorkflowsAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        var payload = new MessageReceivedTriggerPayload(QueueOrTopic, Subscription);
        var correlationId = message.CorrelationId;
        var messageModel = CreateMessageModel(message);
        var input = new Dictionary<string, object> { [MessageReceived.InputKey] = messageModel };
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<MessageReceived>();
        var dispatchRequest = new DispatchTriggerWorkflowsRequest(activityTypeName, payload, correlationId, default, input);
        await _workflowDispatcher.DispatchAsync(dispatchRequest, cancellationToken);
    }

    private ReceivedServiceBusMessageModel CreateMessageModel(ServiceBusReceivedMessage message) =>
        new(
            message.Body.ToArray(),
            message.Subject,
            message.ContentType,
            message.To,
            message.CorrelationId,
            message.DeliveryCount,
            message.EnqueuedTime,
            message.ScheduledEnqueueTime,
            message.ExpiresAt,
            message.LockedUntil,
            message.TimeToLive,
            message.LockToken,
            message.MessageId,
            message.PartitionKey,
            message.TransactionPartitionKey,
            message.ReplyTo,
            message.SequenceNumber,
            message.EnqueuedSequenceNumber,
            message.SessionId,
            message.ReplyToSessionId,
            message.DeadLetterReason,
            message.DeadLetterSource,
            message.DeadLetterErrorDescription,
            message.ApplicationProperties);
}