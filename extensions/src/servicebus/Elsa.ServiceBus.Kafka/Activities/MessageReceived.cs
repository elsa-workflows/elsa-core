using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.ServiceBus.Kafka.Stimuli;
using Elsa.ServiceBus.Kafka.UIHints;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;

namespace Elsa.ServiceBus.Kafka.Activities;

[Activity("Elsa.Kafka", "Kafka", "Executes when a message is received from a given set of topics")]
public class MessageReceived : Trigger<object>
{
    internal const string InputKey = "TransportMessage";

    /// <inheritdoc />
    public MessageReceived([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null)
        : base(source, line) { }

    /// <inheritdoc />
    public MessageReceived(Input<string> consumerDefinitionId, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null)
        : base(source, line)
    {
        ConsumerDefinitionId = consumerDefinitionId;
    }

    /// <summary>
    /// The consumer to read from.
    /// </summary>
    [Input(DisplayName = "Consumer", Description = "The consumer to connect to.", UIHandler = typeof(ConsumerDefinitionsDropdownOptionsProvider), UIHint = InputUIHints.DropDown)]
    public Input<string> ConsumerDefinitionId { get; set; } = null!;

    /// <summary>
    /// The topics to read from.
    /// </summary>
    [Input(DisplayName = "Topics", Description = "The topics to read from.", UIHint = InputUIHints.MultiText)]
    public Input<ICollection<string>> Topics { get; set; } = null!;

    [Input(Description = "Optional. A predicate to filter messages.", AutoEvaluate = false, DefaultSyntax = "JavaScript", UIHint = InputUIHints.ExpressionEditor)]
    public Input<bool> Predicate { get; set; } = null!;

    [Input(DisplayName = "Local", Description = "Whether the event is local to the workflow. When checked, only events delivered to this workflow instance will resume this activity.")]
    public Input<bool> IsLocal { get; set; } = null!;

    /// <summary>
    /// Optional. When set, only Avro messages whose schema full name matches this value will trigger the activity.
    /// Requires the consumer to use <see cref="Factories.AvroConsumerFactory"/>.
    /// Example: <c>com.example.OrderPlaced</c>
    /// </summary>
    [Input(DisplayName = "Schema Full Name", Description = "Optional. Only process messages whose Avro schema full name matches this value. Requires an Avro consumer. Leave empty to process all messages.", UIHandler = typeof(SchemaFullNameDropdownOptionsProvider), UIHint = InputUIHints.DropDown)]
    public Input<string?> SchemaFullName { get; set; } = null!;

    /// <summary>
    /// The received transport message.
    /// </summary>
    public Output<KafkaTransportMessage> TransportMessage = null!;

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
        var body = receivedMessage.Value;
        context.Set(TransportMessage, receivedMessage);
        context.SetResult(body);
    }

    private object GetStimulus(ExpressionExecutionContext context)
    {
        var consumerDefinitionId = ConsumerDefinitionId.Get(context);
        var topics = Topics.GetOrDefault(context) ?? [];
        var isLocal = IsLocal.GetOrDefault(context);
        var activity = context.GetActivity();
        var activityRegistry = context.GetRequiredService<IActivityRegistry>();
        var activityDescriptor = activityRegistry.Find(activity.Type, activity.Version)!;
        var inputDescriptor = activityDescriptor.GetWrappedInputPropertyDescriptor(activity, nameof(Predicate));
        var predicateInput = (Input?)inputDescriptor!.ValueGetter(activity);
        var predicateExpression = predicateInput?.Expression;
        var schemaFullName = SchemaFullName.GetOrDefault(context);

        return new MessageReceivedStimulus
        {
            ConsumerDefinitionId = consumerDefinitionId,
            Topics = topics.Distinct().ToList(),
            IsLocal = isLocal,
            Predicate = predicateExpression,
            SchemaFullName = string.IsNullOrWhiteSpace(schemaFullName) ? null : schemaFullName,
        };
    }
}
