namespace Elsa.MassTransit.Options;

/// <summary>
/// Provides settings to the RabbitMQ broker for MassTransit.
/// </summary>
public class MassTransitActivityOptions
{
    /// <summary>
    /// A set of message types that can be sent and received in the form of workflow activities.
    /// </summary>
    public ISet<Type> MessageTypes { get; set; } = new HashSet<Type>();
}