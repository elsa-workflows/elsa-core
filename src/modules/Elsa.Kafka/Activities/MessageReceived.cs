using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Kafka.Stimuli;
using Elsa.Kafka.UIHints;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Microsoft.Extensions.Options;

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
        DisplayName = "Consumer",
        Description = "The consumer to connect to.",
        UIHandler = typeof(ConsumerDefinitionsDropdownOptionsProvider),
        UIHint = InputUIHints.DropDown
    )]
    public Input<string> ConsumerDefinitionId { get; set; } = default!;
    
    /// <summary>
    /// The topics to read from.
    /// </summary>
    [Input(
        DisplayName = "Topics",
        Description = "The topics to read from.",
        UIHint = InputUIHints.MultiText
    )]
    public Input<ICollection<string>> Topics { get; set; } = default!;

    /// <summary>
    /// Optional. The .NET type to deserialize the message into. Defaults to <see cref="string"/>. 
    /// </summary>
    [Input(Description = "Optional. The .NET type to deserialize the message into.")]
    public Input<Type?> MessageType { get; set; } = default!;

    [Input(
        Description = "Optional. A predicate to filter messages.",
        AutoEvaluate = false
    )]
    public Input<bool> Predicate { get; set; } = default!;
    
    [Input(DisplayName = "Local", Description = "Whether the event is local to the workflow. When checked, only events delivered to this workflow instance will resume this activity.")]
    public Input<bool> IsLocal { get; set; } = default!;

    /// <summary>
    /// The received transport message.
    /// </summary>
    public Output<KafkaTransportMessage> TransportMessage = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // If the activity is triggered by a workflow trigger, resume immediately.
        if (context.IsTriggerOfWorkflow())
        {
            await Resume(context);
        }
        else
        {
            // Otherwise, create a bookmark and wait for the stimulus to arrive.
            context.CreateBookmark(GetStimulus(context.ExpressionExecutionContext), Resume, false);
        }
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
        var deserializer = context.GetRequiredService<IOptions<KafkaOptions>>().Value.Deserializer;
        var serviceProvider = context.WorkflowExecutionContext.ServiceProvider;
        var body = targetType == null ? bodyAsString : deserializer(serviceProvider, bodyAsString, targetType);

        context.Set(TransportMessage, receivedMessage);
        context.SetResult(body);
    }

    private object GetStimulus(ExpressionExecutionContext context)
    {
        var consumerDefinitionId = ConsumerDefinitionId.Get(context);
        var topics = Topics.GetOrDefault(context) ?? [];
        var messageType = MessageType.GetOrDefault(context) ?? typeof(string);
        var isLocal = IsLocal.GetOrDefault(context);
        var activity = context.GetActivity();
        var activityRegistry = context.GetRequiredService<IActivityRegistry>();
        var activityDescriptor = activityRegistry.Find(activity.Type, activity.Version)!;
        var inputDescriptor = activityDescriptor.GetWrappedInputPropertyDescriptor(activity, nameof(Predicate));
        var predicateInput = (Input?)inputDescriptor!.ValueGetter(activity);
        var predicateExpression = predicateInput?.Expression;
        
        return new MessageReceivedStimulus
        {
            ConsumerDefinitionId = consumerDefinitionId,
            Topics = topics.Distinct().ToList(),
            MessageType = messageType,
            IsLocal = isLocal,
            Predicate = predicateExpression,
        };
    }
}