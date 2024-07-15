using System.Runtime.CompilerServices;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Elsa.Common.Contracts;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using JetBrains.Annotations;

namespace Elsa.AzureServiceBus.Activities;

/// Sends a message to a queue or topic in Azure Service Bus.
[Activity("Elsa.AzureServiceBus.Send", "Azure Service Bus", "Send a message to a queue or topic")]
[PublicAPI]
public class SendMessage : CodeActivity
{
    /// <inheritdoc />
    public SendMessage([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// The contents of the message to send.
    [Input(Description = "The contents of the message to send.")]
    public Input<object> MessageBody { get; set; } = default!;

    /// The queue or topic to send the message to.
    public Input<string> QueueOrTopic { get; set; } = default!;

    /// The content type of the message.
    public Input<string>? ContentType { get; set; }

    /// The subject of the message.
    public Input<string>? Subject { get; set; }

    /// The correlation ID of the message.
    public Input<string>? CorrelationId { get; set; }

    /// The formatter to use when serializing the message body.
    public Input<Type?> FormatterType { get; set; } = default!;

    /// The application properties to embed with the Service Bus Message
    [Input(Category = "Advanced",
        DefaultSyntax = "Json",
        SupportedSyntaxes = ["JavaScript", "Json"],
        UIHint = InputUIHints.MultiLine)
    ]
    public Input<IDictionary<string, object>?> ApplicationProperties { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var queueOrTopic = context.Get(QueueOrTopic);
        var messageBody = context.Get(MessageBody);
        var cancellationToken = context.CancellationToken;
        var serializedMessageBody = await SerializeMessageBodyAsync(context, messageBody!, cancellationToken);

        var message = new ServiceBusMessage(serializedMessageBody)
        {
            ContentType = context.Get(ContentType),
            Subject = context.Get(Subject),
            CorrelationId = context.Get(CorrelationId)
        };

        var applicationProperties = ApplicationProperties.GetOrDefault(context);

        if (applicationProperties != null)
            foreach (var property in applicationProperties)
                message.ApplicationProperties.Add(property.Key, ((JsonElement)property.Value).GetString());

        var client = context.GetRequiredService<ServiceBusClient>();
        await using var sender = client.CreateSender(queueOrTopic);
        await sender.SendMessageAsync(message, cancellationToken);
    }

    private async ValueTask<BinaryData> SerializeMessageBodyAsync(ActivityExecutionContext context, object value, CancellationToken cancellationToken)
    {
        if (value is string s) return BinaryData.FromString(s);

        var formatterType = FormatterType.GetOrDefault(context) ?? typeof(JsonFormatter);
        var formatter = context.GetServices<IFormatter>().First(x => x.GetType() == formatterType);
        var data = await formatter.ToStringAsync(value, cancellationToken);

        return BinaryData.FromString(data);
    }
}