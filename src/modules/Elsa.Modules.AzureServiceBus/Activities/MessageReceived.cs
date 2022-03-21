using System.Text.Json.Serialization;
using Elsa.Attributes;
using Elsa.Formatting.Contracts;
using Elsa.Models;
using Elsa.Modules.AzureServiceBus.Models;

namespace Elsa.Modules.AzureServiceBus.Activities;

[Activity("Elsa.AzureServiceBus.MessageReceived", "Executes when a message is received from the configured queue or topic and subscription", "Azure Service Bus")]
public class MessageReceived : Trigger
{
    internal const string MessageReceivedInputKey = "ReceivedMessage";

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
    public Input<string>? Subscription { get; set; } = default!;

    /// <summary>
    /// The expected .NET the received message contains. The received message will be deserialized into this type. Defaults to <see cref="string"/>. 
    /// </summary>
    public Input<Type> ExpectedMessageType { get; set; } = new(typeof(string));

    /// <summary>
    /// The received transport message.
    /// </summary>
    public Output<ReceivedServiceBusMessageModel>? ReceivedMessage { get; set; }

    /// <summary>
    /// The parsed body of the received message. 
    /// </summary>
    public Output<object>? ReceivedMessageBody { get; set; }

    /// <summary>
    /// The formatter to use to parse the message. 
    /// </summary>
    public IFormatter? Formatter { get; set; }

    protected override object GetTriggerDatum(TriggerIndexingContext context) => GetBookmarkData(context.ExpressionExecutionContext);

    protected override void Execute(ActivityExecutionContext context)
    {
        var bookmarkData = GetBookmarkData(context.ExpressionExecutionContext);
        context.CreateBookmark(bookmarkData, Resume);
    }

    private async ValueTask Resume(ActivityExecutionContext context)
    {
        var receivedMessage = (ReceivedServiceBusMessageModel)context.WorkflowExecutionContext.Input[MessageReceivedInputKey]!;
        var bodyAsString = new BinaryData(receivedMessage.Body).ToString();
        var targetType = context.Get(ExpectedMessageType);
        var body = Formatter == null ? bodyAsString : await Formatter.FromStringAsync(bodyAsString, targetType, context.CancellationToken);

        context.Set(ReceivedMessage, receivedMessage);
        context.Set(ReceivedMessageBody, body);
    }

    private object GetBookmarkData(ExpressionExecutionContext context)
    {
        var queueOrTopic = context.Get(QueueOrTopic)!;
        var subscription = context.Get(Subscription);
        return new MessageReceivedTriggerPayload(queueOrTopic, subscription);
    }
}