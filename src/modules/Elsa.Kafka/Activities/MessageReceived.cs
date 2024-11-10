using System.Runtime.CompilerServices;
using System.Text.Json;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Kafka.Stimuli;
using Elsa.Kafka.UIHints;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;

namespace Elsa.Kafka.Activities;

[Activity("Elsa.Kafka", "Kafka", "Executes when a message is received from a given set of topics")]
public class MessageReceived : Trigger<object>
{
    internal const string InputKey = "TransportMessage";

    /// <inheritdoc />
    public MessageReceived([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    public MessageReceived(Input<string> consumerDefinitionId, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        ConsumerDefinitionId = consumerDefinitionId;
    }

    /// <summary>
    /// The consumer to read from.
    /// </summary>
    [Input(
        DisplayName =  "Consumer",
        Description = "The consumer to connect to.",
        UIHandler = typeof(ConsumerDefinitionsDropdownOptionsProvider),
        UIHint = InputUIHints.DropDown
    )]
    public Input<string> ConsumerDefinitionId { get; set; } = default!;
    
    /// <summary>
    /// Optional. The .NET type to deserialize the message into. Defaults to <see cref="string"/>. 
    /// </summary>
    [Input(Description = "Optional. The .NET type to deserialize the message into.")]
    public Input<Type?> MessageType { get; set; } = default!;
    
    /// <summary>
    /// The correlating fields to use when resuming the workflow.
    /// </summary>
    public Input<IDictionary<string, object?>> CorrelatingFields { get; set; } = default!;
    
    /// <summary>
    /// The received transport message.
    /// </summary>
    public Output<KafkaTransportMessage> TransportMessage = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // If the activity is triggered by a workflow trigger, resume immediately.
        if (context.IsTriggerOfWorkflow())
            await Resume(context);
        else
            // Otherwise, create a bookmark and wait for the stimulus to arrive.
            context.CreateBookmark(GetStimulus(context.ExpressionExecutionContext), Resume,false);
    }
    
    /// <inheritdoc />
    protected override object GetTriggerPayload(TriggerIndexingContext context) => GetStimulus(context.ExpressionExecutionContext);

    private async ValueTask Resume(ActivityExecutionContext context)
    {
        var receivedMessage = context.GetWorkflowInput<KafkaTransportMessage>(InputKey);
        SetResult(receivedMessage, context);
        await context.CompleteActivityAsync();
    }

    private void SetResult(KafkaTransportMessage receivedMessage, ActivityExecutionContext context)
    {
        var bodyAsString = receivedMessage.Value;
        var targetType = MessageType.GetOrDefault(context);
        var body = targetType == null ? bodyAsString : JsonSerializer.Deserialize(bodyAsString, targetType)!;

        context.Set(TransportMessage, receivedMessage);
        context.SetResult(body);
    }

    private object GetStimulus(ExpressionExecutionContext context)
    {
        var consumerDefinitionId = ConsumerDefinitionId.Get(context);
        var correlatingFields = CorrelatingFields.GetOrDefault(context) ?? new Dictionary<string, object?>();
        return new MessageReceivedStimulus(consumerDefinitionId, correlatingFields);
    }
}