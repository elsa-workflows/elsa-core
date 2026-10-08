using System.Runtime.CompilerServices;
using Elsa.Mqtt.Contracts;
using Elsa.Mqtt.UIHints;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;

namespace Elsa.Mqtt.Activities;

/// <summary>
/// Publishes a message via MQTT onto a specified topic.
/// </summary>
[Activity(
    Namespace = "Elsa",
    Category = "MQTT",
    DisplayName = "Publish MQTT message",
    Description = "Publishes a message via MQTT onto a specified topic.")]
[FlowNode(OutcomeSuccess, OutcomeFailure)]
public class PublishMqttMessage : Activity<bool>
{
    private const string OutcomeSuccess = "Success";
    private const string OutcomeFailure = "Failure";

    /// <inheritdoc />
    public PublishMqttMessage([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    [Input(
        DisplayName = "Connection Name",
        Description = "The name of the MQTT connection to use, as configured in the module options. Defaults to 'Default'.",
        UIHandler = typeof(MqttConnectionDropdownOptionsProvider),
        UIHint = InputUIHints.DropDown)]
    public Input<string?> ConnectionName { get; set; } = default!;

    [Input(
        DisplayName = "Topic",
        Description = "The name of the topic to publish to.")]
    public Input<string> Topic { get; set; } = default!;

    [Input(
        DisplayName = "Message",
        Description = "The message to publish.",
        UIHint = InputUIHints.MultiLine)]
    public Input<string> Message { get; set; } = default!;

    [Input(
        DisplayName = "QoS",
        Description = "The quality of service level to publish the message with.",
        UIHandler = typeof(MqttQosLevelDropdownOptionsProvider),
        UIHint = InputUIHints.DropDown)]
    public Input<MqttQualityOfServiceLevel> QualityOfServiceLevel { get; set; } = default!;

    [Input(
        DisplayName = "Retain",
        Description = "Whether the broker should retain the message.")]
    public Input<bool> Retain { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var logger = context.GetRequiredService<ILogger<PublishMqttMessage>>();
        var mqttClientFactory = context.GetRequiredService<IMqttClientFactory>();

        var connectionName = context.Get(ConnectionName);
        var topic = context.Get(Topic);
        var message = context.Get(Message);
        var qualityOfServiceLevel = context.Get(QualityOfServiceLevel);
        var retain = context.Get(Retain);

        if (string.IsNullOrWhiteSpace(topic))
        {
            context.AddExecutionLogEntry("Precondition Failed", "The topic name is required (cannot be null or whitespace).");
            context.Set(Result, false);
            await context.CompleteActivityWithOutcomesAsync(OutcomeFailure);
            return;
        }

        if (message == null)
        {
            context.AddExecutionLogEntry("Precondition Failed", "The message is required (cannot be null).");
            context.Set(Result, false);
            await context.CompleteActivityWithOutcomesAsync(OutcomeFailure);
            return;
        }

        var mqttClient = await mqttClientFactory.CreateClientAsync(connectionName);

        var response = await mqttClient.PublishStringAsync(
            topic: topic!,
            payload: message!,
            qualityOfServiceLevel: qualityOfServiceLevel,
            retain: retain,
            cancellationToken: context.CancellationToken);

        if (!response.IsSuccess)
        {
            logger.LogError("MQTT publish failed: {ReasonCode} - {ReasonString}", response.ReasonCode, response.ReasonString);
        }

        var result = response.IsSuccess;

        context.Set(Result, result);

        context.JournalData.Add("ReasonCode", response.ReasonCode);
        context.JournalData.Add("ReasonString", response.ReasonString);
        context.JournalData.Add("Topic", topic!);
        context.JournalData.Add("MessageLength", message!.Length);

        await context.CompleteActivityWithOutcomesAsync(result ? OutcomeSuccess : OutcomeFailure);
    }
}
