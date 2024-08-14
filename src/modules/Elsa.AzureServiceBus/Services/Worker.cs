using Azure.Messaging.ServiceBus;
using Elsa.AzureServiceBus.Activities;
using Elsa.AzureServiceBus.Models;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.AzureServiceBus.Services;

/// <summary>
/// Processes messages received via a queue specified through the <see cref="MessageReceivedStimulus"/>.
/// When a message is received, the appropriate workflows are executed.
/// </summary>
public class Worker : IAsyncDisposable
{
    private readonly ServiceBusProcessor _processor;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Worker"/> class.
    /// </summary>
    public Worker(string queueOrTopic, string? subscription, ServiceBusClient client, IServiceScopeFactory serviceScopeFactory, ILogger<Worker> logger)
    {
        QueueOrTopic = queueOrTopic;
        Subscription = subscription == "" ? default : subscription;
        _serviceScopeFactory = serviceScopeFactory;
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
    /// Starts the worker.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task StartAsync(CancellationToken cancellationToken = default) => await _processor.StartProcessingAsync(cancellationToken);
    
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
        var input = new Dictionary<string, object>
        {
            [MessageReceived.InputKey] = CreateMessageModel(message)
        };

        var metadata = new StimulusMetadata
        {
            CorrelationId = message.CorrelationId,
            Input = input,
        };
        var stimulus = new MessageReceivedStimulus(QueueOrTopic, Subscription);
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var stimulusSender = scope.ServiceProvider.GetRequiredService<IStimulusSender>();
        var result = await stimulusSender.SendAsync<MessageReceived>(stimulus, metadata, cancellationToken);

        _logger.LogDebug("{Count} workflow triggered by the service bus message", result.WorkflowInstanceResponses.Count);
    }

    private static ReceivedServiceBusMessageModel CreateMessageModel(ServiceBusReceivedMessage message)
    {
        return new ReceivedServiceBusMessageModel
        {
            Body = message.Body.ToArray(),
            Subject = message.Subject,
            ContentType = message.ContentType,
            To = message.To,
            CorrelationId = message.CorrelationId,
            DeliveryCount = message.DeliveryCount,
            EnqueuedTime = message.EnqueuedTime,
            ScheduledEnqueuedTime = message.ScheduledEnqueueTime,
            ExpiresAt = message.ExpiresAt,
            LockedUntil = message.LockedUntil,
            TimeToLive = message.TimeToLive,
            LockToken = message.LockToken,
            MessageId = message.MessageId,
            PartitionKey = message.PartitionKey,
            TransactionPartitionKey = message.TransactionPartitionKey,
            ReplyTo = message.ReplyTo,
            SequenceNumber = message.SequenceNumber,
            EnqueuedSequenceNumber = message.EnqueuedSequenceNumber,
            SessionId = message.SessionId,
            ReplyToSessionId = message.ReplyToSessionId,
            DeadLetterReason = message.DeadLetterReason,
            DeadLetterSource = message.DeadLetterSource,
            DeadLetterErrorDescription = message.DeadLetterErrorDescription,
            ApplicationProperties = message.ApplicationProperties
        };
    }
}