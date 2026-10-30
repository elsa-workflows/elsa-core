using Elsa.Mqtt.Options;

namespace Elsa.Mqtt.Stimuli;

/// <summary>
/// The stimulus payload stored on a trigger or bookmark created by <see cref="Activities.MqttMessageReceived"/>.
/// </summary>
public class MqttMessageReceivedStimulus
{
    /// <summary>The name of the MQTT connection to listen on.</summary>
    public required string ConnectionName { get; set; }

    /// <summary>The set of topic filters to subscribe to.</summary>
    public required ICollection<string> Topics { get; set; } = [];
}
