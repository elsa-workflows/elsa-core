using Elsa.Mqtt.Options;
using Elsa.Mqtt.Services;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Elsa.Mqtt.UnitTests.Services;

public class MqttClientFactoryTests
{
    // ---------- CreateClientAsync ----------

    // NOTE: This method is not testable in a unit test because it actually attempts to connect with the created client.

    // ---------- ResolveConnection ----------

    [Fact]
    public void ResolveConnection_ThrowsInvalidOperationException_WhenConnectionNotFound()
    {
        var options = new MqttOptions();
        var factory = new MqttClientFactory(MsOptions.Create(options));

        Assert.Throws<InvalidOperationException>(() => factory.ResolveConnection("non-existent"));
    }

    [Fact]
    public void ResolveConnection_UsesDefaultConnectionName_WhenNameIsNull()
    {
        var connectionOptions = new MqttConnectionOptions { Host = "localhost", Port = 1883 };
        var options = new MqttOptions().AddDefaultConnection(connectionOptions);
        var factory = new MqttClientFactory(MsOptions.Create(options));

        var result = factory.ResolveConnection(null);

        // Should not throw and should return the default connection's built options.
        Assert.NotNull(result);
    }

    [Fact]
    public void ResolveConnection_ReturnsOptions_ForNamedConnection()
    {
        var connectionOptions = new MqttConnectionOptions { Host = "broker.example.com", Port = 8883 };
        var options = new MqttOptions().AddConnection("remote", connectionOptions);
        var factory = new MqttClientFactory(MsOptions.Create(options));

        var result = factory.ResolveConnection("remote");

        Assert.NotNull(result);
    }

    [Fact]
    public void ResolveConnection_IsCaseInsensitive()
    {
        var connectionOptions = new MqttConnectionOptions { Host = "localhost", Port = 1883 };
        var options = new MqttOptions().AddConnection("MyBroker", connectionOptions);
        var factory = new MqttClientFactory(MsOptions.Create(options));

        // All of these should resolve to the same connection.
        Assert.NotNull(factory.ResolveConnection("mybroker"));
        Assert.NotNull(factory.ResolveConnection("MYBROKER"));
        Assert.NotNull(factory.ResolveConnection("MyBroker"));
    }

    [Fact]
    public void ResolveConnection_ErrorMessage_ContainsConnectionName()
    {
        var options = new MqttOptions();
        var factory = new MqttClientFactory(MsOptions.Create(options));

        var ex = Assert.Throws<InvalidOperationException>(() => factory.ResolveConnection("missing-broker"));

        Assert.Contains("missing-broker", ex.Message);
    }
}
