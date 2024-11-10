using Confluent.Kafka;
using Elsa.Extensions;
using Elsa.Kafka.UIHints;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Microsoft.Extensions.Options;

namespace Elsa.Kafka.Activities;

[Activity("Elsa.Kafka", "Kafka", "Sends a message to a given topic")]
public class SendMessage : CodeActivity
{
    /// <summary>
    /// The topic to which the message will be sent.
    /// </summary>
    [Input(
        Description = "The topic to which the message will be sent.",
        UIHint = InputUIHints.DropDown,
        UIHandler = typeof(TopicDefinitionsDropdownOptionsProvider)
    )]
    public Input<string> Topic { get; set; } = default!;
    
    /// <summary>
    /// The producer to use when sending the message.
    /// </summary>
    [Input(
        DisplayName = "Producer",
        Description = "The producer to use when sending the message.",
        UIHint = InputUIHints.DropDown,
        UIHandler = typeof(ProducerDefinitionsDropdownOptionsProvider)
    )]
    public Input<string> ProducerDefinitionId { get; set; } = default!;

    /// <summary>
    /// The content of the message to send.
    /// </summary>
    [Input(Description = "The content of the message to send.")]
    public Input<object> Content { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var topic = Topic.Get(context);
        var producerDefinitionId = ProducerDefinitionId.Get(context);
        var producerDefinitionEnumerator = context.GetRequiredService<IProducerDefinitionEnumerator>();
        var producerDefinition = await producerDefinitionEnumerator.GetByIdAsync(producerDefinitionId);
        var content = Content.Get(context);
        var serializer = context.GetRequiredService<IOptions<KafkaOptions>>().Value.Serializer;
        var serviceProvider = context.WorkflowExecutionContext.ServiceProvider;
        var serializedContent = content as string ?? serializer(serviceProvider, content);
        var config = new ProducerConfig
        {
            BootstrapServers = string.Join(",", producerDefinition.BootstrapServers),
            
        };
        using var producer = new ProducerBuilder<Null, string>(config).Build();
        var result = await producer.ProduceAsync(topic, new Message<Null, string> { Value = serializedContent });
        context.JournalData.Add("MessageId", result.Message.Value);
        context.JournalData.Add("Offset", result.Offset);
        context.JournalData.Add("Status", result.Status);
        context.JournalData.Add("Timestamp", result.Timestamp);
    }
}