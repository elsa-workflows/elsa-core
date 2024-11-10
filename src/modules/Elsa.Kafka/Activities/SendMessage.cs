using System.Text.Json;
using Confluent.Kafka;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Kafka.Activities;

[Activity("Elsa.Kafka", "Kafka", "Sends a message to a given topic")]
public class SendMessage : CodeActivity
{
    /// <summary>
    /// The topic to which the message will be sent.
    /// </summary>
    [Input(Description = "The topic to which the message will be sent.")]
    public Input<string> Topic { get; set; } = default!;

    /// <summary>
    /// The content of the message to send.
    /// </summary>
    public Input<object> Content { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var topic = Topic.Get(context);
        var content = Content.Get(context);
        var serializedContent = content as string ?? JsonSerializer.Serialize(content);
        var config = new ProducerConfig
        {
            BootstrapServers = "localhost:9092",
            
        };
        using var producer = new ProducerBuilder<Null, string>(config).Build();
        var result = await producer.ProduceAsync(topic, new Message<Null, string> { Value = serializedContent });
        context.JournalData.Add("MessageId", result.Message.Value);
        context.JournalData.Add("Offset", result.Offset);
        context.JournalData.Add("Status", result.Status);
        context.JournalData.Add("Timestamp", result.Timestamp);
    }
}