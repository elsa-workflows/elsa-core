using Azure.Messaging.ServiceBus;
using Elsa.Attributes;
using Elsa.Formatting.Formatters;
using Elsa.Formatting.Services;
using Elsa.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.AzureServiceBus.Activities;

[Activity("Elsa.AzureServiceBus.Send", "Azure Service Bus", "Send a message to a queue or topic")]
public class SendMessage : Activity
{
    public Input<object> MessageBody { get; set; } = default!;
    public Input<string> QueueOrTopic { get; set; } = default!;
    public Input<string>? ContentType { get; set; }
    public Input<string>? Subject { get; set; }
    public Input<string>? CorrelationId { get; set; }
    public IFormatter? Formatter { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var queueOrTopic = context.Get(QueueOrTopic);
        var messageBody = context.Get(MessageBody);

        if (!ValidatePreconditions(context, queueOrTopic, messageBody))
            return;

        var cancellationToken = context.CancellationToken;
        var serializedMessageBody = await SerializeMessageBodyAsync(messageBody!, cancellationToken);

        var message = new ServiceBusMessage(serializedMessageBody)
        {
            ContentType = context.Get(ContentType),
            Subject = context.Get(Subject),
            CorrelationId = context.Get(CorrelationId)

            // TODO: Maybe expose additional members.
        };

        var client = context.GetRequiredService<ServiceBusClient>();

        await using var sender = client.CreateSender(queueOrTopic);
        await sender.SendMessageAsync(message, cancellationToken);
    }

    private async ValueTask<BinaryData> SerializeMessageBodyAsync(object value, CancellationToken cancellationToken)
    {
        if (value is string s) return BinaryData.FromString(s);

        var formatter = Formatter ?? new JsonFormatter();
        var data = await formatter.ToStringAsync(value, cancellationToken);

        return BinaryData.FromString(data);
    }

    private static bool ValidatePreconditions(ActivityExecutionContext context, string? queueOrTopic, object? messageBody)
    {
        var logger = context.GetRequiredService<ILogger<SendMessage>>();

        if (string.IsNullOrWhiteSpace(queueOrTopic))
        {
            logger.LogWarning("Can't send a message because no queue or topic was specified");
            return false;
        }

        if (messageBody == null)
        {
            logger.LogWarning("Can't send a message because no message body was specified");
            return false;
        }

        return true;
    }
}