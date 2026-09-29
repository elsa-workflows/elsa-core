using MQTTnet;

namespace Elsa.Mqtt.Utilities;

/// <summary>
/// Provides MQTT topic filter matching according to the MQTT 3.1.1 / 5.0 specification.
/// Supports <c>+</c> (single-level wildcard) and <c>#</c> (multi-level wildcard).
/// </summary>
internal static class MqttTopicMatcher
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="topic"/> matches the subscription <paramref name="filter"/>.
    /// </summary>
    public static bool IsMatch(string filter, string topic)
    {
        return MqttTopicFilterComparer.Compare(topic, filter) == MqttTopicFilterCompareResult.IsMatch;
    }
}
