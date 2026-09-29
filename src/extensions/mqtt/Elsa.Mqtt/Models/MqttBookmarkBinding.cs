using Elsa.Mqtt.Stimuli;

namespace Elsa.Mqtt.Models;

/// <summary>
/// Associates a suspended workflow bookmark with the MQTT stimulus that should resume it.
/// </summary>
public record MqttBookmarkBinding(
    string WorkflowInstanceId,
    string? CorrelationId,
    string BookmarkId,
    MqttMessageReceivedStimulus Stimulus);
