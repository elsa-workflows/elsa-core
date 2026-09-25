namespace Elsa.Server.Web;

/// <summary>
/// Represents the type of messaging broker used in MassTransit.
/// </summary>
public enum MassTransitBroker
{
    Memory,
    AzureServiceBus,
    RabbitMq
}