namespace Elsa.ServiceBus.MassTransit.RabbitMq.Options;

/// <summary>
/// Connection options for the RabbitMQ transport.
/// Provide either <see cref="ConnectionString"/> (AMQP URI) or the individual
/// <see cref="HostName"/> / <see cref="Port"/> / <see cref="VirtualHost"/> /
/// <see cref="UserName"/> / <see cref="Password"/> fields — not both.
/// When <see cref="ConnectionString"/> is set it takes precedence.
/// </summary>
public class RabbitMqOptions
{
    /// <summary>
    /// Full AMQP connection string, e.g.
    /// <c>amqp://guest:guest@localhost:5672/</c> or
    /// <c>amqps://user:pass@rabbit.example.com/my-vhost</c>.
    /// When set, all individual connection fields are ignored.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>The RabbitMQ host name or IP address.</summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>The AMQP port. Defaults to 5672.</summary>
    public ushort Port { get; set; } = 5672;

    /// <summary>The virtual host. Defaults to "/".</summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>The login user name.</summary>
    public string UserName { get; set; } = "guest";

    /// <summary>The login password.</summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Returns the host address as a <see cref="Uri"/> regardless of whether
    /// <see cref="ConnectionString"/> or the individual fields were configured.
    /// </summary>
    public Uri GetHostAddress()
    {
        if (!string.IsNullOrWhiteSpace(ConnectionString))
            return new(ConnectionString);

        var vhost = VirtualHost.TrimStart('/');
        return new($"amqp://{HostName}:{Port}/{vhost}");
    }
}
