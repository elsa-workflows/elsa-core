using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Mqtt.Models;
using Elsa.Mqtt.Options;
using Elsa.Mqtt.Stimuli;
using Elsa.Mqtt.UIHints;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;

namespace Elsa.Mqtt.Activities;

/// <summary>
/// A trigger that executes when a message is received on one or more MQTT topics.
/// </summary>
[Activity(
    Namespace = "Elsa",
    Category = "MQTT",
    DisplayName = "MQTT Message Received",
    Description = "Executes when a message is received on one or more MQTT topics.")]
public class MqttMessageReceived : Trigger<object>
{
    internal const string InputKey = "TransportMessage";

    /// <inheritdoc />
    public MqttMessageReceived([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <summary>The MQTT connection to subscribe on.</summary>
    [Input(
        DisplayName = "Connection Name",
        Description = "The MQTT connection to subscribe on, as configured in the module options. Defaults to 'Default'.",
        UIHandler = typeof(MqttConnectionDropdownOptionsProvider),
        UIHint = InputUIHints.DropDown)]
    public Input<string?> ConnectionName { get; set; } = null!;

    /// <summary>One or more topic filters to listen on. Supports MQTT wildcards (+ and #).</summary>
    [Input(
        DisplayName = "Topics",
        Description = "The topic filters to subscribe to. Supports MQTT single-level (+) and multi-level (#) wildcards.",
        UIHint = InputUIHints.MultiText)]
    public Input<ICollection<string>> Topics { get; set; } = null!;

    /// <summary>The received MQTT message.</summary>
    [Output(
        DisplayName = "Received MQTT Message",
        Description = "An object representing the received MQTT message consisting of Topic and Message.")]
    public Output<MqttMessage> TransportMessage { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if (context.IsTriggerOfWorkflow())
        {
            await ResumeAsync(context);
        }
        else
        {
            context.CreateBookmark(GetStimulus(context.ExpressionExecutionContext), ResumeAsync, includeActivityInstanceId: false);
        }
    }

    /// <inheritdoc />
    protected override object GetTriggerPayload(TriggerIndexingContext context) => GetStimulus(context.ExpressionExecutionContext);

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        var receivedMessage = context.GetWorkflowInput<MqttMessage>(InputKey);

        context.Set(TransportMessage, receivedMessage);
        context.SetResult(receivedMessage.Message);

        await context.CompleteActivityAsync();
    }

    private MqttMessageReceivedStimulus GetStimulus(ExpressionExecutionContext context)
    {
        var connectionName = ConnectionName.GetOrDefault(context) ?? MqttOptions.DefaultConnectionName;
        var topics = Topics.GetOrDefault(context) ?? [];

        if (topics.Count == 0)
        {
            throw new InvalidOperationException("At least one topic filter must be specified.");
        }

        return new MqttMessageReceivedStimulus
        {
            ConnectionName = connectionName,
            Topics = topics.Distinct().ToList(),
        };
    }
}
