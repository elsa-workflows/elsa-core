using System.Runtime.CompilerServices;
using Elsa.AzureServiceBus.Models;
using Elsa.Common.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.AzureServiceBus.Activities;

/// <summary>
/// Triggered when a message is received on a specified queue or topic and subscription.
/// </summary>
[Activity("Elsa.AzureServiceBus", "Azure Service Bus", "Executes when a message is received from the configured queue or topic and subscription")]
public class MessageReceived : Trigger
{
    internal const string InputKey = "TransportMessage";

    /// <inheritdoc />
    public MessageReceived([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    public MessageReceived(Input<string> queue)
    {
        QueueOrTopic = queue;
    }

    /// <inheritdoc />
    public MessageReceived(string queue) : this(new Input<string>(queue))
    {
    }

    /// <inheritdoc />
    public MessageReceived(Input<string> topic, Input<string> subscription)
    {
        QueueOrTopic = topic;
        Subscription = subscription;
    }

    /// <inheritdoc />
    public MessageReceived(string topic, string subscription) : this(new Input<string>(topic), new Input<string>(subscription))
    {
    }

    /// <summary>
    /// The name of the queue or topic to read from.
    /// </summary>
    [Input(Description = "The name of the queue or topic to read from.")]
    public Input<string> QueueOrTopic { get; set; } = default!;
    
    /// <summary>
    /// The name of the subscription to read from.
    /// </summary>
    [Input(Description = "The name of the subscription to read from.")]
    public Input<string>? Subscription { get; set; }

    /// <summary>
    /// The .NET type to deserialize the message into. Defaults to <see cref="string"/>. 
    /// </summary>
    [Input(Description = "The .NET type to deserialize the message into.")]
    public Input<Type> MessageType { get; set; } = new(typeof(string));

    /// <summary>
    /// The received transport message.
    /// </summary>
    [Output(Description = "The received transport message.")]
    public Output<ReceivedServiceBusMessageModel> TransportMessage { get; set; } = default!;
    
    /// <summary>
    /// The received transport message.
    /// </summary>
    [Output(Description = "The received message.")]
    public Output<object> Message { get; set; } = default!;

    /// <summary>
    /// The formatter to use to parse the message. 
    /// </summary>
    [Input(Description = "The formatter to use to serialize the message.")]
    public Input<IFormatter?> Formatter { get; set; } = default!;

    /// <inheritdoc />
    protected override object GetTriggerPayload(TriggerIndexingContext context) => GetBookmarkPayload(context.ExpressionExecutionContext);

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // If we did not receive external input, it means we are just now encountering this activity.
        if (!context.TryGetWorkflowInput<ReceivedServiceBusMessageModel>(InputKey, out var receivedMessage))
        {
            // Create bookmarks for when we receive the expected HTTP request.
            context.CreateBookmark(GetBookmarkPayload(context.ExpressionExecutionContext), Resume);
            return;
        }

        // Provide the received message as output.
        await SetResultAsync(receivedMessage, context);
        await context.CompleteActivityAsync();
    }

    private async ValueTask Resume(ActivityExecutionContext context)
    {
        var receivedMessage = context.GetWorkflowInput<ReceivedServiceBusMessageModel>(InputKey);
        await SetResultAsync(receivedMessage, context);
        await context.CompleteActivityAsync();
    }

    private async Task SetResultAsync(ReceivedServiceBusMessageModel receivedMessage, ActivityExecutionContext context)
    {
        var bodyAsString = new BinaryData(receivedMessage.Body).ToString();
        var targetType = context.Get(MessageType);
        var formatter = Formatter.GetOrDefault(context);
        var body = formatter == null ? bodyAsString : await formatter.FromStringAsync(bodyAsString, targetType, context.CancellationToken);

        context.Set(TransportMessage, receivedMessage);
        context.Set(Message, body);
    }

    private object GetBookmarkPayload(ExpressionExecutionContext context)
    {
        var queueOrTopic = context.Get(QueueOrTopic)!;
        var subscription = context.Get(Subscription);
        return new MessageReceivedTriggerPayload(queueOrTopic, subscription);
    }
}