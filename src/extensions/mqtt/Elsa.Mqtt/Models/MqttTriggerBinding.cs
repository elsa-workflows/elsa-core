using Elsa.Mqtt.Stimuli;
using Elsa.Workflows.Activities;

namespace Elsa.Mqtt.Models;

/// <summary>
/// Associates a workflow trigger with the MQTT stimulus that should activate it.
/// </summary>
public record MqttTriggerBinding(
    Workflow Workflow,
    string TriggerId,
    string TriggerActivityId,
    MqttMessageReceivedStimulus Stimulus);
