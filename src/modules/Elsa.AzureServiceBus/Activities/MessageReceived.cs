using System.Text.Json.Serialization;
using Elsa.AzureServiceBus.Models;
using Elsa.Common.Services;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.AzureServiceBus.Activities;

[Activity("Elsa.AzureServiceBus.MessageReceived", "Azure Service Bus", "Executes when a message is received from the configured queue or topic and subscription")]
public class MessageReceived : Trigger<object>
{
    internal const string InputKey = "ReceivedMessage";

    [JsonConstructor]
    public MessageReceived()
    {
    }

    public MessageReceived(Input<string> queue)
    {
        QueueOrTopic = queue;
    }

    public MessageReceived(string queue) : this(new Input<string>(queue))
    {
    }

    public MessageReceived(Input<string> topic, Input<string> subscription)
    {
        QueueOrTopic = topic;
        Subscription = subscription;
    }

    public MessageReceived(string topic, string subscription) : this(new Input<string>(topic), new Input<string>(subscription))
    {
    }

    public Input<string> QueueOrTopic { get; set; } = default!;
    public Input<string>? Subscription { get; set; }

    /// <summary>
    /// The expected .NET the received message contains. The received message will be deserialized into this type. Defaults to <see cref="string"/>. 
    /// </summary>
    public Input<Type> ExpectedMessageType { get; set; } = new(typeof(string));

    /// <summary>
    /// The received transport message.
    /// </summary>
    public Output<ReceivedServiceBusMessageModel>? ReceivedMessage { get; set; }

    /// <summary>
    /// The formatter to use to parse the message. 
    /// </summary>
    public IFormatter? Formatter { get; set; }

    protected override object GetTriggerDatum(TriggerIndexingContext context) => GetBookmarkPayload(context.ExpressionExecutionContext);

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // If we did not receive external input, it means we are just now encountering this activity.
        if (!context.TryGetInput<ReceivedServiceBusMessageModel>(InputKey, out var receivedMessage))
        {
            // Create bookmarks for when we receive the expected HTTP request.
            context.CreateBookmark(GetBookmarkPayload(context.ExpressionExecutionContext), Resume);
            return;
        }

        // Provide the received message as output.
        await SetResultAsync(receivedMessage, context);
    }

    private async ValueTask Resume(ActivityExecutionContext context)
    {
        var receivedMessage = context.GetInput<ReceivedServiceBusMessageModel>(InputKey);
        await SetResultAsync(receivedMessage, context);
    }

    private async Task SetResultAsync(ReceivedServiceBusMessageModel receivedMessage, ActivityExecutionContext context)
    {
        var bodyAsString = new BinaryData(receivedMessage.Body).ToString();
        var targetType = context.Get(ExpectedMessageType);
        var body = Formatter == null ? bodyAsString : await Formatter.FromStringAsync(bodyAsString, targetType, context.CancellationToken);

        context.Set(ReceivedMessage, receivedMessage);
        context.Set(Result, body);
    }

    private object GetBookmarkPayload(ExpressionExecutionContext context)
    {
        var queueOrTopic = context.Get(QueueOrTopic)!;
        var subscription = context.Get(Subscription);
        return new MessageReceivedTriggerPayload(queueOrTopic, subscription);
    }
}