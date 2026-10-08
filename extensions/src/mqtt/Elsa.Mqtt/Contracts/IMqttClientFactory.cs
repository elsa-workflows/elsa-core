using MQTTnet;

namespace Elsa.Mqtt.Contracts;

internal interface IMqttClientFactory
{
    /// <summary>
    /// Creates and binds an MQTT client using the <paramref name="connectionName"/> for config retrieval.
    /// If no <paramref name="connectionName"/> is provided, the <c>Default</c> connection is used.
    /// </summary>
    public Task<IMqttClient> CreateClientAsync(string? connectionName = null, CancellationToken cancellationToken = default);
}
