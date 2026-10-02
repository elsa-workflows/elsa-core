using MQTTnet;

namespace Elsa.Mqtt.Options;

/// <summary>
/// Options to configure the MQTT client with.
/// </summary>
public class MqttOptions
{
    internal const string DefaultConnectionName = "Default";

    /// <summary>
    /// Named MQTT connections. The key is the connection name used in activities.
    /// A connection named <c>Default</c> is used when no connection name is specified.
    /// </summary>
    internal Dictionary<string, MqttClientOptions> Connections { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a named MQTT connection.
    /// </summary>
    public MqttOptions AddConnection(string name, MqttClientOptions options)
    {
        Connections[name] = options;
        return this;
    }

    /// <summary>
    /// Registers a named MQTT connection.
    /// </summary>
    public MqttOptions AddConnection(string name, MqttConnectionOptions options)
    {
        Connections[name] = options.GenerateMqttClientOptions();
        return this;
    }

    /// <summary>
    /// Registers the default MQTT connection (name <c>Default</c>).
    /// </summary>
    public MqttOptions AddDefaultConnection(MqttClientOptions options) => AddConnection(DefaultConnectionName, options);

    /// <summary>
    /// Registers the default MQTT connection (name <c>Default</c>).
    /// </summary>
    public MqttOptions AddDefaultConnection(MqttConnectionOptions options) => AddConnection(DefaultConnectionName, options.GenerateMqttClientOptions());

    /// <summary>
    /// Maximum number of reconnect attempts after an unexpected disconnection.
    /// Set to <c>0</c> for unlimited retries (default).
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 0;

    /// <summary>
    /// Base delay for the first reconnect attempt. Subsequent attempts use exponential
    /// back-off capped at 5 minutes. Defaults to 5 seconds.
    /// </summary>
    public TimeSpan ReconnectBaseDelay { get; set; } = TimeSpan.FromSeconds(5);
}
