using Elsa.Mqtt.Contracts;
using MQTTnet;

namespace Elsa.Mqtt.UnitTests;

// Fake for the internal IMqttClientFactory interface (InternalsVisibleTo grants access).
internal sealed class FakeMqttClientFactory(IMqttClient client) : IMqttClientFactory
{
    public Task<IMqttClient> CreateClientAsync(string? connectionName = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(client);
}
