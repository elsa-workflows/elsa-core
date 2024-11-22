using System.Text;
using Confluent.Kafka;
using Elsa.Extensions;
using Elsa.Kafka.UIHints;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.UIHints;
using Microsoft.Extensions.Options;

namespace Elsa.Kafka.Activities;

[Activity("Elsa.Kafka", "Kafka", "Produces a message and delivers it to a given topic.")]
public class ProduceMessage : CodeActivity
{
    /// <summary>
    /// The topic to which the message will be sent.
    /// </summary>
    [Input(
        Description = "The topic to which the message will be delivered.",
        UIHint = InputUIHints.DropDown,
        UIHandler = typeof(TopicDefinitionsDropdownOptionsProvider)
    )]
    public Input<string> Topic { get; set; } = default!;

    /// <summary>
    /// The producer to use when sending the message.
    /// </summary>
    [Input(
        DisplayName = "Producer",
        Description = "The producer to use to produce the message.",
        UIHint = InputUIHints.DropDown,
        UIHandler = typeof(ProducerDefinitionsDropdownOptionsProvider)
    )]
    public Input<string> ProducerDefinitionId { get; set; } = default!;

    [Input(DisplayName = "Local", Description = "When checked, the message will be delivered to this workflow instance only.")]
    public Input<bool> IsLocal { get; set; } = default!;

    /// <summary>
    /// Optional. The correlation ID to assign to the message.
    /// </summary>
    [Input(
        DisplayName = "Correlation ID",
        Description = "Optional. The correlation ID to assign to the message."
    )]
    public Input<string?> CorrelationId { get; set; } = default!;

    /// <summary>
    /// The content of the message to send.
    /// </summary>
    [Input(Description = "The content of the message to produce.")]
    public Input<object> Content { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var topic = Topic.Get(context);
        var producerDefinitionId = ProducerDefinitionId.Get(context);
        var producerDefinitionEnumerator = context.GetRequiredService<IProducerDefinitionEnumerator>();
        var producerDefinition = await producerDefinitionEnumerator.GetByIdAsync(producerDefinitionId);
        var content = Content.Get(context);

        context.DeferTask(async () =>
        {
            using var producer = CreateProducer(context, producerDefinition);
            var headers = CreateHeaders(context);
            await producer.ProduceAsync(topic, content, headers);
        });
    }

    private Headers CreateHeaders(ActivityExecutionContext context)
    {
        var options = context.GetRequiredService<IOptions<KafkaOptions>>().Value;
        var headers = new Headers();
        var correlationId = CorrelationId.GetOrDefault(context);
        var isLocal = IsLocal.Get(context);

        if (!string.IsNullOrWhiteSpace(correlationId))
            headers.Add(options.CorrelationHeaderKey, Encoding.UTF8.GetBytes(correlationId));

        if (isLocal)
            headers.Add(options.WorkflowInstanceIdHeaderKey, Encoding.UTF8.GetBytes(context.WorkflowExecutionContext.Id));

        return headers;
    }
    
    private IProducer CreateProducer(ActivityExecutionContext context, ProducerDefinition producerDefinition)
    {
        var factory = context.GetRequiredService(producerDefinition.FactoryType) as IProducerFactory;
        
        if (factory == null)
            throw new InvalidOperationException($"Producer factory of type '{producerDefinition.FactoryType}' not found.");
        
        var createProducerContext = new CreateProducerContext(producerDefinition);
        return factory.CreateProducer(createProducerContext);
    }
}