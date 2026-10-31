using Elsa.Mqtt.Contracts;
using Elsa.Mqtt.Options;
using Microsoft.Extensions.Options;
using MQTTnet;

namespace Elsa.Mqtt.Services;

internal class MqttClientFactory : IMqttClientFactory, IDisposable
{
    private readonly MqttOptions _options;
    private readonly Dictionary<string, IMqttClient> _connections = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _semaphore = new(1);
    
    private bool _disposed;

    public MqttClientFactory(IOptions<MqttOptions> options)
    {
        _options = options.Value;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                foreach (var connection in _connections.Values)
                {
                    connection.Dispose();
                }

                _connections.Clear();
                _semaphore.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public async Task<IMqttClient> CreateClientAsync(string? connectionName = null, CancellationToken cancellationToken = default)
    {
        connectionName ??= MqttOptions.DefaultConnectionName;

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            if (_connections.TryGetValue(connectionName, out var connection))
            {
                return connection;
            }

            var connectionOptions = ResolveConnection(connectionName);
            var newConnection = new MQTTnet.MqttClientFactory().CreateMqttClient();

            try
            {
                var response = await newConnection.ConnectAsync(connectionOptions, cancellationToken);

                if (response?.ResultCode != MqttClientConnectResultCode.Success)
                    throw new InvalidOperationException(
                        $"Failed to connect to MQTT broker for connection '{connectionName}'. Result code: {response?.ResultCode}");

                _connections.Add(connectionName, newConnection);

                return newConnection;
            }
            catch
            {
                newConnection.Dispose();
                throw;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Resolves a named <see cref="MqttConnectionOptions"/> from the configured options.
    /// </summary>
    public MqttClientOptions ResolveConnection(string? connectionName)
    {
        var name = connectionName ?? MqttOptions.DefaultConnectionName;

        if (!_options.Connections.TryGetValue(name, out var connectionOptions))
        {
            throw new InvalidOperationException(
                $"No MQTT connection named '{name}' was found. " +
                "Configure connections using UseMqtt(mqtt => mqtt.ConfigureOptions = o => o.AddConnection(...)).");
        }

        return connectionOptions;
    }
}
