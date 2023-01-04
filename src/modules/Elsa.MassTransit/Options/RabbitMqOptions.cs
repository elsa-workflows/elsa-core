namespace Elsa.MassTransit.Options;

/// <summary>
/// Provides settings to the RabbitMQ broker for MassTransit.
/// </summary>
public class RabbitMqOptions
{
    public string Host { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}